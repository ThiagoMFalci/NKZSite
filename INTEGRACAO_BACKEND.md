# 🔌 GUIA DE INTEGRAÇÃO COM BACKEND

## 📡 Conectando a uma API

Este guia mostra como conectar o Kitsune Academy a um backend real.

---

## 🚀 Estrutura Recomendada

### Backend em Node.js/Express
```
backend/
├── server.js
├── routes/
│   ├── auth.js       # Login/Registro
│   ├── tournaments.js # Torneios
│   ├── teams.js      # Times
│   └── users.js      # Usuários
├── models/
│   ├── User.js
│   ├── Tournament.js
│   └── Team.js
└── config/
    └── database.js
```

---

## 🔐 Autenticação (JWT)

### 1. Modificar o sistema de login

**Arquivo: interactive.js**

```javascript
// Antes (cliente-side apenas)
function handleLogin() {
    const email = document.querySelector('#registerEmail')?.value;
    notifications.show(`Bem-vindo, ${email}!`, 'success');
    ModalSystem.close();
}

// Depois (com API)
async function handleLogin() {
    const email = document.querySelector('#registerEmail')?.value;
    const password = document.querySelector('#registerPassword')?.value;
    
    try {
        const response = await fetch('http://api.seu-dominio.com/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        
        const data = await response.json();
        
        if (data.token) {
            localStorage.setItem('authToken', data.token);
            localStorage.setItem('user', JSON.stringify(data.user));
            notifications.show('Login realizado com sucesso!', 'success');
            ModalSystem.close();
            setTimeout(() => window.location.href = 'dashboard.html', 1000);
        } else {
            notifications.show(data.message || 'Erro no login', 'error');
        }
    } catch (error) {
        notifications.show('Erro ao conectar ao servidor', 'error');
    }
}
```

---

## 📋 Endpoints Sugeridos

### Autenticação
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/logout
GET    /api/auth/me
```

### Torneios
```
GET    /api/tournaments           # Listar torneios
GET    /api/tournaments/:id       # Detalhes
POST   /api/tournaments           # Criar
POST   /api/tournaments/:id/join  # Participar
```

### Times
```
GET    /api/teams                 # Listar times
GET    /api/teams/:id             # Detalhes
POST   /api/teams                 # Criar
POST   /api/teams/:id/join        # Entrar
```

### Usuários
```
GET    /api/users/:id             # Perfil
PUT    /api/users/:id             # Atualizar
GET    /api/users/:id/stats       # Estatísticas
```

---

## 🔗 Exemplo: Buscar Torneios da API

### Antes (dados estáticos)
```javascript
// interactive.js
const SITE_DATA = {
    tournaments: [
        { id: 1, name: 'Campeonato...', ... },
        { id: 2, name: 'Torneio...', ... }
    ]
};
```

### Depois (dados dinâmicos)
```javascript
// Adicionar ao DOMContentLoaded
async function loadTournamentsFromAPI() {
    try {
        const response = await fetch('http://api.seu-dominio.com/api/tournaments');
        const tournaments = await response.json();
        
        // Renderizar torneios
        const container = document.querySelector('.tournaments-grid');
        tournaments.forEach(tournament => {
            const card = createTournamentCard(tournament);
            container.appendChild(card);
        });
    } catch (error) {
        console.error('Erro ao buscar torneios:', error);
        notifications.show('Erro ao carregar torneios', 'error');
    }
}

function createTournamentCard(tournament) {
    const card = document.createElement('div');
    card.className = 'tournament-card reveal';
    card.innerHTML = `
        <div class="tournament-header">
            <h3>${tournament.name}</h3>
            <span class="badge ${tournament.status}">${tournament.status}</span>
        </div>
        <div class="tournament-info">
            <p><i class="fas fa-users"></i> ${tournament.teams} Equipes</p>
            <p><i class="fas fa-dollar-sign"></i> ${tournament.prize}</p>
        </div>
        <button class="btn-participate" onclick="joinTournament(${tournament.id})">
            INSCREVER
        </button>
    `;
    return card;
}

// Chamar ao carregar dashboard
if (window.location.pathname.includes('tournaments')) {
    loadTournamentsFromAPI();
}
```

---

## 💾 Salvar Dados do Usuário

### Ao fazer login, armazenar token e dados
```javascript
async function handleLogin() {
    // ... código anterior ...
    
    if (response.ok) {
        const data = await response.json();
        
        // Armazenar token e dados do usuário
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('userId', data.user.id);
        localStorage.setItem('userName', data.user.name);
        localStorage.setItem('userRank', data.user.rank);
        
        // Atualizar SITE_DATA
        SITE_DATA.user = data.user;
        
        // Redirecionar
        window.location.href = 'dashboard.html';
    }
}
```

### Ao carregar a página, restaurar dados
```javascript
function restoreUserSession() {
    const authToken = localStorage.getItem('authToken');
    
    if (!authToken) {
        // Usuário não logado
        return;
    }
    
    // Restaurar dados do usuário
    SITE_DATA.user = {
        id: localStorage.getItem('userId'),
        name: localStorage.getItem('userName'),
        rank: localStorage.getItem('userRank'),
        // ... outros dados
    };
    
    // Se estiver no dashboard, ir para login
    if (window.location.pathname.includes('dashboard') && !authToken) {
        window.location.href = 'index.html';
    }
}

// Chamar no DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    restoreUserSession();
    // ... resto do código
});
```

---

## 🌐 Configuração CORS

### No seu backend (Node.js/Express)
```javascript
const cors = require('cors');

app.use(cors({
    origin: 'http://localhost:8000',
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    credentials: true
}));
```

### Em produção
```javascript
app.use(cors({
    origin: process.env.FRONTEND_URL,
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    credentials: true
}));
```

---

## 🔄 Real-time com WebSocket

### Servidor WebSocket (Node.js)
```javascript
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

wss.on('connection', (ws) => {
    ws.on('message', (data) => {
        const event = JSON.parse(data);
        
        if (event.type === 'tournament-update') {
            // Enviar para todos os clientes
            wss.clients.forEach(client => {
                if (client.readyState === WebSocket.OPEN) {
                    client.send(JSON.stringify(event));
                }
            });
        }
    });
});
```

### Cliente (interactive.js)
```javascript
class WebSocketManager {
    constructor() {
        this.ws = new WebSocket('ws://localhost:8080');
        this.ws.onmessage = (event) => this.handleMessage(event);
    }
    
    handleMessage(event) {
        const data = JSON.parse(event.data);
        
        if (data.type === 'tournament-update') {
            notifications.show(`Novo torneio: ${data.tournament.name}!`, 'info');
            loadTournamentsFromAPI(); // Recarregar
        }
        
        if (data.type === 'user-online') {
            updateRealtimeCounters(data);
        }
    }
    
    send(message) {
        if (this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(message));
        }
    }
}

// Inicializar
let wsManager;
document.addEventListener('DOMContentLoaded', () => {
    wsManager = new WebSocketManager();
});
```

---

## 📊 Exemplo Completo: Dashboard com API

```javascript
// Carregar dashboard do usuário autenticado
async function loadUserDashboard() {
    const authToken = localStorage.getItem('authToken');
    
    if (!authToken) {
        window.location.href = 'index.html';
        return;
    }
    
    try {
        const response = await fetch('http://api.seu-dominio.com/api/users/me', {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        const user = await response.json();
        
        // Atualizar SITE_DATA
        SITE_DATA.user = user;
        
        // Animar stats
        DashboardInteractive.updateStats();
        
        // Carregar torneios do usuário
        loadUserTournaments(user.id);
        
        // Carregar times do usuário
        loadUserTeams(user.id);
        
    } catch (error) {
        console.error('Erro ao carregar dashboard:', error);
        notifications.show('Erro ao carregar dados', 'error');
    }
}

async function loadUserTournaments(userId) {
    try {
        const response = await fetch(
            `http://api.seu-dominio.com/api/users/${userId}/tournaments`
        );
        const tournaments = await response.json();
        
        const container = document.querySelector('.my-tournaments');
        container.innerHTML = '';
        
        tournaments.forEach(tournament => {
            const card = createTournamentCard(tournament);
            container.appendChild(card);
        });
    } catch (error) {
        console.error('Erro ao buscar torneios:', error);
    }
}

// Chamar ao carregar dashboard
if (window.location.pathname.includes('dashboard')) {
    loadUserDashboard();
}
```

---

## 🔐 Proteção com JWT

### Adicionar interceptor de requisições
```javascript
async function apiCall(url, options = {}) {
    const authToken = localStorage.getItem('authToken');
    
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };
    
    if (authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
    }
    
    try {
        const response = await fetch(url, {
            ...options,
            headers
        });
        
        if (response.status === 401) {
            // Token expirado
            localStorage.removeItem('authToken');
            window.location.href = 'index.html';
        }
        
        return response.json();
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// Usar em lugar de fetch
const data = await apiCall('http://api.seu-dominio.com/api/tournaments');
```

---

## 🚀 Deploy

### Variáveis de Ambiente (.env)
```
REACT_APP_API_URL=https://api.seu-dominio.com
REACT_APP_WS_URL=wss://ws.seu-dominio.com
```

### Usar no código
```javascript
const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:3001';

async function getTournaments() {
    const response = await fetch(`${API_URL}/api/tournaments`);
    return response.json();
}
```

---

## 📝 Checklist de Integração

- [ ] Backend criado e testado
- [ ] CORS configurado
- [ ] Endpoints de autenticação funcionando
- [ ] JWT tokens sendo gerados
- [ ] Endpoints de torneios e times prontos
- [ ] Database conectado
- [ ] Modificar handleLogin() com API
- [ ] Modificar loadTournaments com API
- [ ] Adicionar proteção de rotas
- [ ] Testar fluxo completo
- [ ] Deploy em produção
- [ ] Monitorar erros
- [ ] Otimizar performance

---

## 🎯 Próximas Melhorias

1. **Refresh Token**: Renovar tokens automaticamente
2. **Rate Limiting**: Proteger contra ataques
3. **Logging**: Rastrear ações dos usuários
4. **Analytics**: Coletar dados de uso
5. **Backup**: Sistema de backup automático
6. **Cache**: Redis para performance
7. **CDN**: Distribuir conteúdo globalmente
8. **Monitoring**: Alertas de erro em tempo real

---

**Status**: 📖 Guia de Integração Completo  
**Versão**: 1.0  
**Para Dúvidas**: Consulte a documentação do seu backend
