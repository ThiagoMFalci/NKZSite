# NKZ Discord Bot

Bot separado da API para verificacao de Discord e futuras automacoes do servidor NKZ.

## Fluxo atual

1. O usuario cria conta no site informando o Discord ID.
2. A API gera um codigo de 6 digitos.
3. A API chama `POST /verification/send` neste bot usando `NKZ_BOT_SECRET`.
4. O bot verifica se o usuario esta no servidor `DISCORD_GUILD_ID`.
5. Se estiver, envia o codigo por DM.
6. O usuario confirma o codigo no site.

## Variaveis

Copie `.env.example` para `.env`:

```env
DISCORD_BOT_TOKEN=
DISCORD_GUILD_ID=
NKZ_BOT_SECRET=troque-por-uma-chave-grande
PORT=3001
```

Na API, configure:

```env
DiscordBot__BaseUrl=https://url-do-bot
DiscordBot__Secret=mesma-chave-do-NKZ_BOT_SECRET
Discord__ServerInviteUrl=https://discord.gg/seu-convite
```

## Rodar local

```bash
npm install
npm run dev
```

O bot precisa estar no servidor NKZ e ter intent de membros habilitada no Developer Portal.
