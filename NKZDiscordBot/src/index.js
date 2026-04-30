import "dotenv/config";
import express from "express";
import { Client, GatewayIntentBits } from "discord.js";

const {
  DISCORD_BOT_TOKEN,
  DISCORD_GUILD_ID,
  NKZ_BOT_SECRET,
  PORT = 3001,
} = process.env;

if (!DISCORD_BOT_TOKEN) throw new Error("DISCORD_BOT_TOKEN is required.");
if (!DISCORD_GUILD_ID) throw new Error("DISCORD_GUILD_ID is required.");
if (!NKZ_BOT_SECRET) throw new Error("NKZ_BOT_SECRET is required.");

const client = new Client({
  intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMembers],
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

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    ready: client.isReady(),
    bot: client.user?.tag || null,
  });
});

app.post("/verification/send", requireInternalSecret, async (req, res) => {
  try {
    if (!client.isReady()) {
      return res.status(503).json({ message: "Discord bot is not ready." });
    }

    const discordUserId = normalizeDiscordUserId(req.body.discordUserId);
    const code = String(req.body.code || "").trim();
    const email = String(req.body.email || "").trim();
    const serverInviteUrl = String(req.body.serverInviteUrl || "").trim();

    if (!/^\d{17,20}$/.test(discordUserId) || !/^\d{6}$/.test(code)) {
      return res.status(400).json({ message: "Invalid Discord user or code." });
    }

    const guild = await client.guilds.fetch(DISCORD_GUILD_ID);
    let member = null;
    try {
      member = await guild.members.fetch(discordUserId);
    } catch {
      return res.status(400).json({
        message: serverInviteUrl
          ? `Entre no servidor NKZ antes de criar a conta: ${serverInviteUrl}`
          : "Entre no servidor NKZ antes de criar a conta.",
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

    return res.json({ success: true });
  } catch (error) {
    return res.status(500).json({
      message: error?.message || "Could not send Discord verification.",
    });
  }
});

client.once("ready", () => {
  console.log(`NKZ Discord bot online as ${client.user.tag}`);
});

await client.login(DISCORD_BOT_TOKEN);

app.listen(Number(PORT), () => {
  console.log(`NKZ Discord bot API listening on ${PORT}`);
});
