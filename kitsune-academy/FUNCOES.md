# 🎮 Kitsune Academy - Guia de Funções do Site

## 📋 Resumo Executivo

Todas as funcionalidades do site agora estão implementadas! Aqui está um guia completo de todas as funções disponíveis nos botões e interações.

---

## 🔐 Funções de Autenticação

### `openRegisterModal()`
**Descrição:** Abre o modal de registro/login do usuário
**Onde é usada:** Botão "ENTRAR" no header de todas as páginas
**Funcionalidades:**
- Formulário de nome de usuário
- Entrada de email
- Criação de senha
- Seleção de time (opcional)
- Aceitar termos de serviço

---

## 👤 Funções de Perfil

### `editProfile()`
**Descrição:** Abre o modal para editar perfil do usuário
**Dados que pode editar:**
- Nome de usuário
- Tag do jogo (nick)
- Rank atual
- Time
- Foto de perfil (URL)
- Bio pessoal

### `logout()`
**Descrição:** Realiza logout do usuário
**Comportamento:**
- Confirma se deseja sair
- Mostra mensagem de despedida
- Redireciona para homepage

---

## 📊 Funções de Estatísticas

### `viewStats()`
**Descrição:** Exibe as estatísticas detalhadas do jogador
**Informações exibidas:**
- Partidas jogadas
- Taxa de vitória
- Número de vitórias
- Número de derrotas
- Campeão principal

---

## 🏆 Funções de Torneios

### `joinTournament(tournamentId)`
**Parâmetro:** `tournamentId` - ID do torneio
**Descrição:** Inscreve o jogador em um torneio
**Informações do modal:**
- Nome do torneio
- Status (ativo, em breve, finalizando)
- Prêmios
- Inscritos
- Data de início

### `searchTournaments()`
**Descrição:** Abre um prompt para buscar torneios
**Funcionalidades:**
- Busca por palavras-chave
- Retorna torneios encontrados
- Mostra detalhes de cada um

### `createTournament()`
**Descrição:** Abre modal para criar um novo torneio
**Campos obrigatórios:**
- Nome do torneio
- Número de equipes
- Prêmio total (R$)
- Data de início
- Data limite de inscrição
- Descrição

### `learnMore(topic)`
**Parâmetro:** `topic` - 'tournaments', 'teams', 'ranking'
**Descrição:** Exibe informações educacionais
**Tópicos disponíveis:**
- Como participar de torneios
- Como criar um time
- Como entender o ranking

---

## 👥 Funções de Times

### `viewTeam(teamName)`
**Parâmetro:** `teamName` - Nome do time
**Descrição:** Exibe detalhes completos do time
**Informações exibidas:**
- Rank do time
- Vitórias
- Win rate
- Número de jogadores
- Escalação (5 jogadores com lanes)

### `invitePlayers()`
**Descrição:** Abre modal para convidar jogadores
**Funcionalidades:**
- Adicionar nicks de jogadores (separados por vírgula)
- Mensagem personalizada
- Notificação imediata
- Suporte para múltiplos convites

---

## ⚙️ Funções de Configuração

### `openSettings()`
**Descrição:** Abre modal com todas as configurações
**Opções disponíveis:**
- ✅ Notificações ativas
- ✅ Sons ativados
- ✅ Mostrar online
- ✅ Tema Escuro
- ✅ Perfil público

### `reportBug()`
**Descrição:** Abre formulário para reportar bugs
**Campos:**
- Tipo de problema (bug, erro, sugestão, outro)
- Descrição detalhada
- Página afetada
- Screenshot (opcional)

### `contactSupport()`
**Descrição:** Abre formulário de contato com suporte
**Campos:**
- Email do usuário
- Assunto
- Mensagem
- Checkbox de privacidade

---

## 📱 Funções de Menu

**Mobile Menu Toggle** (Automático)
- Botão de menu no header em dispositivos mobile
- Abre/fecha navegação responsiva
- Fecha automaticamente ao clicar em um link

---

## 🔗 Funções de Compartilhamento

### `shareContent()`
**Descrição:** Abre opções para compartilhar o site
**Plataformas:**
- 📘 Facebook
- 𝕏 Twitter
- in LinkedIn
- 🔗 Copiar Link (e copiar para clipboard)

---

## 📈 Funções de Dashboard (Se Autenticado)

### `DashboardInteractive.updateStats()`
- Anima números de estatísticas
- Atualiza dados em tempo real
- Exibe matches, win rate, vitórias

### `DashboardInteractive.addQuickActionListeners()`
- Detecta cliques em quick actions
- Mostra notificações contextuais
- Abre modais baseado no botão

---

## 🎯 Sistema de Notificações

### `NotificationSystem.show(message, type, duration)`
**Parâmetros:**
- `message` (string) - Mensagem a exibir
- `type` (string) - 'success', 'error', 'warning', 'info'
- `duration` (ms) - Tempo de exibição (default 3000ms)

**Exemplo:**
```javascript
NotificationSystem.show('Conta criada com sucesso! 🎉', 'success', 3000);
```

---

## 📋 Sistema de Modais

### `ModalSystem.create(title, content, actions)`
**Parâmetros:**
- `title` (string) - Título do modal
- `content` (string) - Conteúdo HTML
- `actions` (array) - Botões de ação

**Exemplo:**
```javascript
ModalSystem.create('Bem-vindo!', '<p>Bem-vindo ao site</p>', [
    { label: 'Fechar', type: 'secondary' },
    { label: 'OK', type: 'primary', callback: () => console.log('OK') }
]);
```

---

## 🎨 Dados Globais

### `SITE_DATA`
Objeto global contendo:

```javascript
SITE_DATA = {
    user: {
        name: 'Igor Parente',
        rank: 'Diamante II',
        team: 'Kitsune Legends',
        stats: {
            matches: 127,
            winRate: 62,
            victories: 79,
            defeats: 48
        }
    },
    tournaments: [...],
    teams: [...]
}
```

---

## 🌟 Funções de Filtro

### `FilterSystem.filterTournaments(status)`
- Filtra torneios por status: 'all', 'active', 'running', 'upcoming'

### `FilterSystem.searchTeams(query)`
- Busca times por nome

### `FilterSystem.sortTeams(sortBy)`
- Ordena times por: 'rank', 'wr' (win rate)

---

## 💡 Exemplos de Uso

### Abrir modal de registro e criar conta
```javascript
openRegisterModal(); // Botão "ENTRAR"
```

### Participar de um torneio
```javascript
joinTournament(1); // ID do torneio
```

### Ver detalhes de um time
```javascript
viewTeam('KSN Esports');
```

### Convidar jogadores
```javascript
invitePlayers();
```

### Acessar configurações
```javascript
openSettings();
```

### Compartilhar o site
```javascript
shareContent();
```

---

## 📂 Arquivos do Sistema

### Arquivos principais:
- `index.html` - Homepage
- `dashboard.html` - Painel de controle
- `tournaments.html` - Página de torneios
- `teams.html` - Gerenciador de times
- `champions.html` - Estatísticas de campeões
- `stats.html` - Estatísticas detalhadas
- `matches.html` - Histórico de partidas

### Arquivos JavaScript:
- `interactive.js` - Sistema de modais e notificações
- `main.js` - Efeitos de botões e animações
- `functions.js` - **TODAS AS FUNÇÕES DO SITE** ⭐
- `charts.js` - Gráficos (dashboard)

### Estilo:
- `style.css` - Animações personalizadas e efeitos especiais

---

## 🎮 Funcionalidades em Produção

✅ **Implementadas:**
- ✅ Registro e autenticação
- ✅ Modal de login
- ✅ Edição de perfil
- ✅ Criação de torneios
- ✅ Inscrição em torneios
- ✅ Gerenciamento de times
- ✅ Convite de jogadores
- ✅ Busca de torneios
- ✅ Configurações
- ✅ Reportar bugs
- ✅ Contato com suporte
- ✅ Compartilhamento
- ✅ Menu mobile responsivo
- ✅ Efeitos de hover
- ✅ Animações de scroll
- ✅ Notificações em tempo real

---

## 🔔 Notificações Automáticas (Em produção)

O sistema exibe automaticamente:
- "Nova partida disponível!"
- "Time convidou você para Scrim!"
- "Torneio começará em 1 hora"
- "Você subiu 15 LP!"

---

## 🚀 Como Usar no Seu Projeto

### 1. Incluir os scripts nas páginas HTML:
```html
<script src="interactive.js"></script>
<script src="main.js"></script>
<script src="functions.js"></script>
```

### 2. Chamar funções nos botões:
```html
<button onclick="openRegisterModal()">Registrar</button>
<button onclick="logout()">Sair</button>
<button onclick="openSettings()">Configurações</button>
```

### 3. Usar em JavaScript customizado:
```javascript
// Mostrar notificação
NotificationSystem.show('Ação realizada!', 'success');

// Criar modal customizado
ModalSystem.create('Titulo', '<p>Conteúdo</p>', [
    { label: 'Confirmar', type: 'primary', callback: () => {} }
]);
```

---

## 📞 Suporte

Para questões ou adições de novas funções, use:
- `reportBug()` - Para bugs
- `contactSupport()` - Para suporte
- `learnMore('tournaments')` - Para dúvidas

---

## 📝 Notas de Desenvolvimento

- Todos os dados estão em `SITE_DATA`
- As notificações desaparecem automaticamente após 3 segundos
- Os modais podem ser fechados clicando no X ou fora do modal
- Todas as funções retornam para a página atual (sem reload)
- Sistema totalmente responsivo para mobile

---

**Criado em:** 28 de Janeiro de 2026  
**Versão:** 1.0.0  
**Status:** ✅ Produção
