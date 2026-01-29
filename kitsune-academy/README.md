# 🎮 Kitsune Academy - Plataforma de Esports

Sistema completo de gerenciamento de torneios, times e competições de League of Legends.

## 📁 Estrutura de Arquivos

```
kitsune-academy/
├── index.html              # Página inicial
├── tournaments.html        # Página de torneios
├── teams.html             # Página de times
├── dashboard.html         # Dashboard pessoal
├── style.css              # Estilos globais
├── main.js                # Scripts gerais
└── tournament-logo.svg    # Logo de criar torneios
```

## 🚀 Como Rodar

### Opção 1: Localmente com Python (Recomendado)
```bash
cd /Users/igorparente/Projetos/NKZ/NKZSite/kitsune-academy
python -m http.server 8000
```
Depois acesse: **http://localhost:8000**

### Opção 2: Localmente com Node.js
```bash
npx http-server
```
Depois acesse: **http://localhost:8080**

### Opção 3: Abrir Diretamente no Navegador
- Abra o arquivo `index.html` diretamente no seu navegador
- ⚠️ Alguns recursos podem não funcionar corretamente

## 📄 Páginas Disponíveis

### 🏠 Home (index.html)
- Seção Hero com título "DOMINE O RIFT"
- Recursos de Nível Pro (6 cards)
- Vídeos do YouTube (Highlights, Torneios, Gameplay)
- Call-to-action para criar conta

### 🏆 Torneios (tournaments.html)
- Lista de torneios ativos
- Barra de progresso de inscrições
- 3 exemplos: Campeonato Brasileiro, Solo Ranking, Scrim League
- Badges de status (Inscrições Abertas, Em Andamento, Em Breve)

### 👥 Times (teams.html)
- Grid de top 6 times
- Rank, Win Rate, Estatísticas
- Cards com banners coloridos
- Botões para ver times detalhados

### 📊 Dashboard (dashboard.html)
- Perfil do jogador
- Estatísticas pessoais
- Últimas partidas
- Próximos torneios
- Ações rápidas

## 🎨 Cores Utilizadas

- **Primária**: Roxo (#9c27b0)
- **Secundária**: Azul (#5c6cff)
- **Terciária**: Laranja (#ff5722)
- **Background**: Escuro (#0a0e27)

## ✨ Features Implementados

✅ Design Gaming Cyberpunk
✅ Efeito Glitch no título principal
✅ Animações fluidas e transições
✅ Responsive design (mobile-friendly)
✅ Navegação entre páginas
✅ Cards interativas com hover effects
✅ Barra de progresso de torneios
✅ Vídeos do YouTube integrados
✅ Dark mode nativo
✅ Icons FontAwesome

## 📱 Responsividade

- ✅ Desktop (1200px+)
- ✅ Tablet (768px - 1199px)
- ✅ Mobile (< 768px)

## 🎥 Vídeos Inclusos

Os vídeos são embedados do YouTube:
1. League of Legends Highlights - GN1oDN56sBc
2. Tournament Highlights - c6rKbUYtvR4
3. Gameplay Guide - 8KLfVVsaXJo

## 🔧 Tecnologias

- HTML5
- CSS3 (Grid, Flexbox, Gradientes)
- JavaScript (Vanilla)
- FontAwesome Icons
- Google Fonts (Orbitron, Audiowide, Roboto)

## 📝 Notas Importantes

- Use um servidor web local para evitar problemas de CORS com vídeos
- Os vídeos do YouTube funcionam melhor em servidores HTTP/HTTPS
- O site é totalmente responsivo e funciona em qualquer dispositivo

## 🎯 Próximos Passos

- [ ] Integração com backend (Node.js/Express)
- [ ] Banco de dados (MongoDB/PostgreSQL)
- [ ] Sistema de login/autenticação
- [ ] Gerenciamento real de torneios
- [ ] Chat em tempo real
- [ ] Notificações push
- [ ] Mobile app com React Native

---

**Desenvolvido com ❤️ para a Kitsune Academy**
