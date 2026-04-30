import "dotenv/config";
import express from "express";
import { ChannelType, Client, GatewayIntentBits, GatewayDispatchEvents, RESTEvents } from "discord.js";

const DISCORD_BOT_TOKEN = process.env.DISCORD_BOT_TOKEN?.trim();
const DISCORD_GUILD_ID = process.env.DISCORD_GUILD_ID?.trim();
const NKZ_BOT_SECRET = process.env.NKZ_BOT_SECRET?.trim();
const PORT = process.env.PORT?.trim() || 3001;

const missingConfig = [
  ["DISCORD_BOT_TOKEN", DISCORD_BOT_TOKEN],
  ["DISCORD_GUILD_ID", DISCORD_GUILD_ID],
  ["NKZ_BOT_SECRET", NKZ_BOT_SECRET],
].filter(([, value]) => !value).map(([key]) => key);

const diagnostics = {
  bootedAt: new Date().toISOString(),
  loginStatus: "not-started",
  loginStartedAt: null,
  readyAt: null,
  lastError: null,
  lastWarning: null,
  lastDebug: null,
  lastGatewayEvent: null,
  restCheck: null,
  guildCheck: null,
  logs: [],
};

function redact(value) {
  if (!value) return null;
  const text = String(value);
  if (text.length <= 8) return "***";
  return `${text.slice(0, 4)}...${text.slice(-4)}`;
}

function addDiagnosticLog(level, message, detail = null) {
  const entry = {
    at: new Date().toISOString(),
    level,
    message,
    detail: detail ? String(detail).slice(0, 500) : null,
  };
  diagnostics.logs.push(entry);
  diagnostics.logs = diagnostics.logs.slice(-25);
  const line = detail ? `${message} ${detail}` : message;
  if (level === "error") console.error(line);
  else if (level === "warn") console.warn(line);
  else console.log(line);
}

const client = new Client({
  intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMembers],
  failIfNotExists: false,
  rest: {
    retries: 1,
    timeout: 15000,
  },
});

const app = express();
app.use(express.json({ limit: "32kb" }));

function requireInternalSecret(req, res, next) {
  const header = req.headers.authorization || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : "";
  if (token !== NKZ_BOT_SECRET) {
    return res.status(401).json({ message: "Unauthorized" });
  }
  return next();
}

function normalizeDiscordUserId(value) {
  return String(value || "").replace(/\D/g, "");
}

function normalizeDiscordUsername(value) {
  return String(value || "").trim().replace(/^@/, "").toLowerCase();
}

function normalizeRoleName(teamName, teamTag) {
  const tag = String(teamTag || "").trim().toUpperCase();
  const name = String(teamName || "Time").trim().replace(/\s+/g, " ").slice(0, 64);
  return tag ? `NKZ ${tag} - ${name}`.slice(0, 90) : `NKZ ${name}`.slice(0, 90);
}

async function resolveGuildMember(guild, identity) {
  const raw = String(identity || "").trim();
  const discordUserId = normalizeDiscordUserId(raw);

  if (/^\d{17,20}$/.test(discordUserId)) {
    return guild.members.fetch(discordUserId);
  }

  const username = normalizeDiscordUsername(raw);
  if (!username) return null;

  const exactCached = guild.members.cache.find((member) => (
    member.user.username.toLowerCase() === username ||
    member.user.tag.toLowerCase() === username ||
    member.displayName.toLowerCase() === username
  ));
  if (exactCached) return exactCached;

  const results = await guild.members.search({ query: username, limit: 10 });
  const exact = results.find((member) => (
    member.user.username.toLowerCase() === username ||
    member.user.tag.toLowerCase() === username ||
    member.displayName.toLowerCase() === username
  ));

  if (exact) return exact;
  if (results.size === 1) return results.first();
  return null;
}

app.get("/health", (_req, res) => {
  res.status(missingConfig.length ? 500 : 200).json({
    ok: missingConfig.length === 0,
    ready: client.isReady(),
    bot: client.user?.tag || null,
    loginStatus: diagnostics.loginStatus,
    readyAt: diagnostics.readyAt,
    missingConfig,
    configPreview: {
      guildId: DISCORD_GUILD_ID || null,
      token: redact(DISCORD_BOT_TOKEN),
      secret: redact(NKZ_BOT_SECRET),
    },
    guildCheck: diagnostics.guildCheck,
    restCheck: diagnostics.restCheck,
    lastError: diagnostics.lastError,
    lastWarning: diagnostics.lastWarning,
    lastDebug: diagnostics.lastDebug,
    lastGatewayEvent: diagnostics.lastGatewayEvent,
    logs: diagnostics.logs,
  });
});

app.get("/debug/rest-check", async (_req, res) => {
  const result = await checkDiscordRest();
  res.status(result.ok ? 200 : 500).json(result);
});

app.get("/debug/network", async (_req, res) => {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10000);

  try {
    const startedAt = Date.now();
    const response = await fetch("https://discord.com/api/v10/gateway", {
      signal: controller.signal,
      headers: { "User-Agent": "NKZDiscordBot/1.0" },
    });
    const body = await response.json().catch(() => null);
    return res.status(response.ok ? 200 : 500).json({
      ok: response.ok,
      status: response.status,
      elapsedMs: Date.now() - startedAt,
      body,
    });
  } catch (error) {
    return res.status(500).json({
      ok: false,
      error: error?.name === "AbortError" ? "Discord network check timed out after 10 seconds." : (error?.message || String(error)),
    });
  } finally {
    clearTimeout(timeout);
  }
});

app.get("/debug/gateway", async (_req, res) => {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10000);

  try {
    const startedAt = Date.now();
    const response = await fetch("https://discord.com/api/v10/gateway/bot", {
      signal: controller.signal,
      headers: {
        Authorization: `Bot ${DISCORD_BOT_TOKEN}`,
        "User-Agent": "NKZDiscordBot/1.0",
      },
    });
    const body = await response.json().catch(() => null);
    return res.status(response.ok ? 200 : 500).json({
      ok: response.ok,
      status: response.status,
      elapsedMs: Date.now() - startedAt,
      retryAfter: body?.retry_after || null,
      message: body?.message || null,
      hasUrl: Boolean(body?.url),
      sessionStartLimit: body?.session_start_limit || null,
    });
  } catch (error) {
    return res.status(500).json({
      ok: false,
      error: error?.name === "AbortError" ? "Discord gateway check timed out after 10 seconds." : (error?.message || String(error)),
    });
  } finally {
    clearTimeout(timeout);
  }
});

app.post("/debug/login", requireInternalSecret, async (_req, res) => {
  if (client.isReady()) {
    return res.json({ success: true, message: "Bot already ready.", bot: client.user?.tag });
  }

  startDiscordLogin();
  return res.json({ success: true, message: "Login attempt started.", loginStatus: diagnostics.loginStatus });
});

app.post("/verification/send", requireInternalSecret, async (req, res) => {
  try {
    if (missingConfig.length) {
      return res.status(500).json({ message: `Missing config: ${missingConfig.join(", ")}` });
    }

    if (!client.isReady()) {
      return res.status(503).json({ message: "Discord bot is not ready." });
    }

    const discordIdentity = String(req.body.discordIdentity || req.body.discordUserId || req.body.discordUsername || "").trim();
    const code = String(req.body.code || "").trim();
    const email = String(req.body.email || "").trim();
    const serverInviteUrl = String(req.body.serverInviteUrl || "").trim();

    if (!discordIdentity || !/^\d{6}$/.test(code)) {
      return res.status(400).json({ message: "Invalid Discord user or code." });
    }

    const guild = await client.guilds.fetch(DISCORD_GUILD_ID);
    let member = null;
    try {
      member = await resolveGuildMember(guild, discordIdentity);
    } catch {
      member = null;
    }

    if (!member) {
      return res.status(400).json({
        message: serverInviteUrl
          ? `Entre no servidor NKZ antes de criar a conta: ${serverInviteUrl}`
          : "Entre no servidor NKZ e confira se digitou seu usuario do Discord corretamente.",
      });
    }

    await member.send([
      "**NKZ Academy - verificacao de conta**",
      "",
      `Codigo: **${code}**`,
      email ? `Conta: ${email}` : "",
      "",
      "Digite este codigo no site para confirmar que este Discord e seu.",
      "O codigo expira em 15 minutos.",
    ].filter(Boolean).join("\n"));

    return res.json({
      success: true,
      discordUserId: member.user.id,
      discordUsername: member.user.globalName ? `${member.user.globalName} (@${member.user.username})` : member.user.username,
    });
  } catch (error) {
    return res.status(500).json({
      message: error?.message || "Could not send Discord verification.",
    });
  }
});

app.post("/discord/bootstrap", requireInternalSecret, async (_req, res) => {
  try {
    if (!client.isReady()) {
      return res.status(503).json({ message: "Discord bot is not ready." });
    }

    const guild = await client.guilds.fetch(DISCORD_GUILD_ID);
    const channels = await guild.channels.fetch();
    let category = channels.find((channel) =>
      channel?.type === ChannelType.GuildCategory &&
      channel.name.toLowerCase() === "ligas"
    );

    if (!category) {
      category = await guild.channels.create({
        name: "ligas",
        type: ChannelType.GuildCategory,
        reason: "NKZ bootstrap",
      });
    }

    return res.json({ success: true, categoryId: category.id, categoryName: category.name });
  } catch (error) {
    return res.status(500).json({ message: error?.message || "Could not bootstrap Discord guild." });
  }
});

app.post("/teams/sync-role", requireInternalSecret, async (req, res) => {
  try {
    if (!client.isReady()) {
      return res.status(503).json({ message: "Discord bot is not ready." });
    }

    const teamName = String(req.body.teamName || "Time").trim();
    const teamTag = String(req.body.teamTag || "").trim().toUpperCase();
    const players = Array.isArray(req.body.players) ? req.body.players : [];
    const desiredUserIds = new Set(players
      .map((player) => normalizeDiscordUserId(player.discordUserId))
      .filter((id) => /^\d{17,20}$/.test(id)));

    const guild = await client.guilds.fetch(DISCORD_GUILD_ID);
    const roleName = normalizeRoleName(teamName, teamTag);
    let roles = await guild.roles.fetch();
    let role = roles.find((item) => item.name === roleName);

    if (!role) {
      role = await guild.roles.create({
        name: roleName,
        mentionable: true,
        reason: `NKZ team role for ${teamName}`,
      });
    }

    const assigned = [];
    const failed = [];

    for (const userId of desiredUserIds) {
      try {
        const member = await guild.members.fetch(userId);
        if (!member.roles.cache.has(role.id)) {
          await member.roles.add(role, `NKZ team sync: ${teamName}`);
        }
        assigned.push(userId);
      } catch (error) {
        failed.push({ userId, message: error?.message || String(error) });
      }
    }

    const currentMembers = await guild.members.fetch();
    const removed = [];
    for (const member of currentMembers.values()) {
      if (member.roles.cache.has(role.id) && !desiredUserIds.has(member.id)) {
        await member.roles.remove(role, `NKZ team sync: ${teamName}`);
        removed.push(member.id);
      }
    }

    return res.json({
      success: true,
      roleId: role.id,
      roleName: role.name,
      assigned,
      removed,
      failed,
    });
  } catch (error) {
    return res.status(500).json({ message: error?.message || "Could not sync team role." });
  }
});

client.once("ready", () => {
  diagnostics.loginStatus = "ready";
  diagnostics.readyAt = new Date().toISOString();
  addDiagnosticLog("info", `NKZ Discord bot online as ${client.user.tag}`);
  checkGuildAccess();
});

client.on("error", (error) => {
  diagnostics.lastError = error?.stack || error?.message || String(error);
  addDiagnosticLog("error", "Discord client error:", diagnostics.lastError);
});

client.on("warn", (warning) => {
  diagnostics.lastWarning = String(warning);
  addDiagnosticLog("warn", "Discord warning:", diagnostics.lastWarning);
});

client.on("debug", (debug) => {
  diagnostics.lastDebug = String(debug).slice(0, 500);
  if (
    debug.includes("gateway") ||
    debug.includes("Session") ||
    debug.includes("Identifying") ||
    debug.includes("Heartbeat")
  ) {
    addDiagnosticLog("info", "Discord debug:", debug);
  }
});

client.rest.on(RESTEvents.Debug, (message) => {
  diagnostics.lastDebug = String(message).slice(0, 500);
  addDiagnosticLog("info", "Discord REST debug:", diagnostics.lastDebug);
});

client.rest.on(RESTEvents.RateLimited, (info) => {
  diagnostics.lastWarning = `Discord REST rate limited: ${JSON.stringify(info).slice(0, 500)}`;
  addDiagnosticLog("warn", diagnostics.lastWarning);
});

client.rest.on(RESTEvents.Response, (_request, _response, data) => {
  const status = data?.status ?? data?.response?.status;
  if (status && status >= 400) {
    diagnostics.lastWarning = `Discord REST response status ${status}`;
    addDiagnosticLog("warn", diagnostics.lastWarning);
  }
});

client.ws.on(GatewayDispatchEvents.Ready, () => {
  diagnostics.lastGatewayEvent = "READY";
  addDiagnosticLog("info", "Discord gateway READY dispatch received.");
});

async function checkGuildAccess() {
  try {
    const guild = await client.guilds.fetch(DISCORD_GUILD_ID);
    diagnostics.guildCheck = {
      ok: true,
      id: guild.id,
      name: guild.name,
      checkedAt: new Date().toISOString(),
    };
    addDiagnosticLog("info", `Discord guild access ok: ${guild.name} (${guild.id})`);
  } catch (error) {
    diagnostics.guildCheck = {
      ok: false,
      error: error?.message || String(error),
      checkedAt: new Date().toISOString(),
    };
    addDiagnosticLog("error", "Discord guild access failed:", diagnostics.guildCheck.error);
  }
}

async function checkDiscordRest() {
  if (!DISCORD_BOT_TOKEN) {
    diagnostics.restCheck = {
      ok: false,
      error: "DISCORD_BOT_TOKEN is missing.",
      checkedAt: new Date().toISOString(),
    };
    return diagnostics.restCheck;
  }

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10000);

  try {
    const response = await fetch("https://discord.com/api/v10/users/@me", {
      headers: {
        Authorization: `Bot ${DISCORD_BOT_TOKEN}`,
        "User-Agent": "NKZDiscordBot/1.0",
      },
      signal: controller.signal,
    });

    const body = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(body?.message || `Discord REST returned HTTP ${response.status}`);
    }

    const currentBot = body;
    diagnostics.restCheck = {
      ok: true,
      id: currentBot.id,
      username: currentBot.username,
      checkedAt: new Date().toISOString(),
    };
    addDiagnosticLog("info", `Discord REST token ok: ${currentBot.username} (${currentBot.id})`);
    return diagnostics.restCheck;
  } catch (error) {
    const isAbort = error?.name === "AbortError";
    diagnostics.restCheck = {
      ok: false,
      error: isAbort ? "Discord REST check timed out after 10 seconds." : (error?.message || String(error)),
      code: error?.code || null,
      status: error?.status || null,
      checkedAt: new Date().toISOString(),
    };
    diagnostics.lastError = diagnostics.restCheck.error;
    addDiagnosticLog("error", "Discord REST token check failed:", diagnostics.restCheck.error);
    return diagnostics.restCheck;
  } finally {
    clearTimeout(timeout);
  }
}

async function startDiscordLogin() {
  if (missingConfig.length) {
    diagnostics.loginStatus = "missing-config";
    addDiagnosticLog("error", `Missing required config: ${missingConfig.join(", ")}`);
    return;
  }

  if (diagnostics.loginStatus === "starting") {
    addDiagnosticLog("warn", "Discord login already starting.");
    return;
  }

  diagnostics.loginStatus = "starting";
  diagnostics.loginStartedAt = new Date().toISOString();
  diagnostics.lastError = null;
  addDiagnosticLog("info", "Starting Discord login.");

  const timeout = setTimeout(() => {
    if (!client.isReady()) {
      diagnostics.loginStatus = "timeout";
      diagnostics.lastError = "Discord login did not become ready within 45 seconds.";
      addDiagnosticLog("error", diagnostics.lastError);
    }
  }, 45000);

  client.login(DISCORD_BOT_TOKEN).catch((error) => {
    clearTimeout(timeout);
    diagnostics.loginStatus = "failed";
    diagnostics.lastError = error?.stack || error?.message || String(error);
    addDiagnosticLog("error", "Discord login failed:", diagnostics.lastError);
  });
}

process.on("unhandledRejection", (error) => {
  diagnostics.lastError = error?.stack || error?.message || String(error);
  addDiagnosticLog("error", "Unhandled rejection:", diagnostics.lastError);
});

process.on("uncaughtException", (error) => {
  diagnostics.lastError = error?.stack || error?.message || String(error);
  addDiagnosticLog("error", "Uncaught exception:", diagnostics.lastError);
});

app.listen(Number(PORT), "0.0.0.0", () => {
  addDiagnosticLog("info", `NKZ Discord bot API listening on ${PORT}`);
  addDiagnosticLog("info", `Node.js version: ${process.version}`);
  addDiagnosticLog("info", `Config preview: guild=${DISCORD_GUILD_ID || "missing"} token=${redact(DISCORD_BOT_TOKEN)} secret=${redact(NKZ_BOT_SECRET)}`);
  startDiscordLogin();
});
