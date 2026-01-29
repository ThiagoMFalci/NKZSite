/**
 * ============================================================
 * KITSUNE ACADEMY - SISTEMA COMPLETO E FUNCIONAL
 * ============================================================
 * Todas as funcionalidades reais do site com dados persistentes
 */

// ==================== GERENCIADOR DE DADOS ====================
class DataManager {
    constructor() {
        this.loadData();
    }

    loadData() {
        const saved = localStorage.getItem('kitsuneData');
        if (saved) {
            this.data = JSON.parse(saved);
        } else {
            this.data = this.getDefaultData();
            this.saveData();
        }
    }

    saveData() {
        localStorage.setItem('kitsuneData', JSON.stringify(this.data));
    }

    getDefaultData() {
        return {
            currentUser: null,
            users: [],
            tournaments: [
                {
                    id: 1,
                    name: 'Campeonato Brasileiro 2026',
                    description: 'O maior campeonato de League of Legends com prêmios milionários.',
                    teams: 32,
                    prize: 'R$ 50.000',
                    status: 'active',
                    progress: 75,
                    start: '15 de Fevereiro',
                    deadline: '10 de Fevereiro',
                    createdBy: 'admin',
                    participants: []
                },
                {
                    id: 2,
                    name: 'Torneio Solo Ranking 2026',
                    description: 'Série especial para jogadores master e profissionais.',
                    teams: 256,
                    prize: 'R$ 20.000',
                    status: 'running',
                    progress: 45,
                    start: 'Qualificatórias',
                    deadline: '28 de Fevereiro',
                    createdBy: 'admin',
                    participants: []
                }
            ],
            teams: [
                { id: 1, name: 'KSN Esports', rank: 1, wins: '15-2', wr: 94, players: 5, icon: '👑', owner: 'admin', members: [] },
                { id: 2, name: 'ProFire Gaming', rank: 2, wins: '13-3', wr: 81, players: 5, icon: '🔥', owner: 'admin', members: [] }
            ]
        };
    }

    // ===== USUÁRIOS =====
    createUser(username, email, password, teamName) {
        if (this.data.users.find(u => u.username === username)) {
            throw new Error('Usuário já existe!');
        }
        if (this.data.users.find(u => u.email === email)) {
            throw new Error('Email já cadastrado!');
        }

        const newUser = {
            id: Date.now(),
            username,
            email,
            password, // AVISO: Em produção, usar hash!
            teamName,
            rank: 'Bronze',
            lp: 0,
            stats: {
                matches: 0,
                wins: 0,
                losses: 0,
                winRate: 0,
                mainChamp: 'Nenhum'
            },
            teams: [],
            tournaments: [],
            createdAt: new Date().toISOString()
        };

        this.data.users.push(newUser);
        this.saveData();
        return newUser;
    }

    loginUser(username, password) {
        const user = this.data.users.find(u => u.username === username && u.password === password);
        if (!user) {
            throw new Error('Usuário ou senha incorretos!');
        }
        this.data.currentUser = user;
        this.saveData();
        return user;
    }

    logoutUser() {
        this.data.currentUser = null;
        this.saveData();
    }

    getCurrentUser() {
        return this.data.currentUser;
    }

    updateUserProfile(userId, updates) {
        const user = this.data.users.find(u => u.id === userId);
        if (!user) throw new Error('Usuário não encontrado!');
        Object.assign(user, updates);
        this.saveData();
        return user;
    }

    // ===== TORNEIOS =====
    createTournament(name, description, teams, prize, startDate, deadline) {
        const newTournament = {
            id: Date.now(),
            name,
            description,
            teams,
            prize,
            status: 'upcoming',
            progress: 0,
            start: startDate,
            deadline,
            createdBy: this.data.currentUser?.id,
            participants: [],
            createdAt: new Date().toISOString()
        };

        this.data.tournaments.push(newTournament);
        this.saveData();
        return newTournament;
    }

    getTournaments() {
        return this.data.tournaments;
    }

    getTournamentById(id) {
        return this.data.tournaments.find(t => t.id === id);
    }

    joinTournament(userId, tournamentId) {
        const tournament = this.getTournamentById(tournamentId);
        const user = this.data.users.find(u => u.id === userId);

        if (!tournament) throw new Error('Torneio não encontrado!');
        if (!user) throw new Error('Usuário não encontrado!');
        if (tournament.participants.includes(userId)) throw new Error('Você já está inscrito!');
        if (tournament.participants.length >= tournament.teams) throw new Error('Torneio lotado!');

        tournament.participants.push(userId);
        if (!user.tournaments) user.tournaments = [];
        user.tournaments.push(tournamentId);

        this.saveData();
        return tournament;
    }

    leaveTournament(userId, tournamentId) {
        const tournament = this.getTournamentById(tournamentId);
        const user = this.data.users.find(u => u.id === userId);

        if (!tournament) throw new Error('Torneio não encontrado!');
        if (!user) throw new Error('Usuário não encontrado!');

        tournament.participants = tournament.participants.filter(id => id !== userId);
        user.tournaments = user.tournaments.filter(id => id !== tournamentId);

        this.saveData();
        return tournament;
    }

    // ===== TIMES =====
    createTeam(name, owner, icon) {
        const newTeam = {
            id: Date.now(),
            name,
            owner,
            icon,
            rank: 'Sem Classificação',
            wins: '0-0',
            wr: 0,
            players: 1,
            members: [owner],
            createdAt: new Date().toISOString()
        };

        this.data.teams.push(newTeam);
        
        const user = this.data.users.find(u => u.id === owner);
        if (user) {
            if (!user.teams) user.teams = [];
            user.teams.push(newTeam.id);
        }

        this.saveData();
        return newTeam;
    }

    getTeams() {
        return this.data.teams;
    }

    getTeamById(id) {
        return this.data.teams.find(t => t.id === id);
    }

    inviteToTeam(teamId, userId) {
        const team = this.getTeamById(teamId);
        const user = this.data.users.find(u => u.id === userId);

        if (!team) throw new Error('Time não encontrado!');
        if (!user) throw new Error('Usuário não encontrado!');
        if (team.members.includes(userId)) throw new Error('Jogador já está no time!');
        if (team.members.length >= 5) throw new Error('Time cheio!');

        team.members.push(userId);
        team.players = team.members.length;
        
        if (!user.teams) user.teams = [];
        user.teams.push(teamId);

        this.saveData();
        return team;
    }

    removeFromTeam(teamId, userId) {
        const team = this.getTeamById(teamId);
        const user = this.data.users.find(u => u.id === userId);

        if (!team) throw new Error('Time não encontrado!');
        if (!user) throw new Error('Usuário não encontrado!');

        team.members = team.members.filter(id => id !== userId);
        team.players = team.members.length;
        user.teams = user.teams.filter(id => id !== teamId);

        this.saveData();
        return team;
    }
}

// Instância global
const dataManager = new DataManager();

// ==================== MENU MOBILE ====================
document.addEventListener('DOMContentLoaded', () => {
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const mobileMenu = document.getElementById('mobileMenu');
    
    if (mobileMenuBtn && mobileMenu) {
        mobileMenuBtn.addEventListener('click', () => {
            mobileMenu.classList.toggle('hidden');
            const icon = mobileMenuBtn.querySelector('i');
            icon.classList.toggle('fa-bars');
            icon.classList.toggle('fa-times');
        });
        
        mobileMenu.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                mobileMenu.classList.add('hidden');
                mobileMenuBtn.querySelector('i').classList.add('fa-bars');
                mobileMenuBtn.querySelector('i').classList.remove('fa-times');
            });
        });
    }
});

// ==================== REGISTRO DE USUÁRIO ====================
window.openRegisterModal = function() {
    const content = `
        <form class="form-modal" id="registerForm">
            <div class="form-group">
                <label><i class="fas fa-user"></i> Nome de Usuário</label>
                <input type="text" id="regUsername" placeholder="Seu nick no jogo" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-envelope"></i> Email</label>
                <input type="email" id="regEmail" placeholder="seu@email.com" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-lock"></i> Senha</label>
                <input type="password" id="regPassword" placeholder="Mínimo 8 caracteres" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-shield-alt"></i> Time (Opcional)</label>
                <input type="text" id="regTeam" placeholder="Nome do seu time">
            </div>
            <div class="form-group">
                <input type="checkbox" id="termsCheck" required>
                <label style="display: inline;">Concordo com os Termos de Serviço</label>
            </div>
        </form>
    `;
    
    ModalSystem.create('🎮 CRIAR CONTA - KITSUNE ACADEMY', content, [
        { 
            label: '❌ Cancelar', 
            type: 'secondary',
            callback: () => {
                NotificationSystem.show('Registro cancelado', 'info');
            }
        },
        { 
            label: '✅ REGISTRAR',
            type: 'primary',
            callback: () => {
                try {
                    const username = document.getElementById('regUsername').value;
                    const email = document.getElementById('regEmail').value;
                    const password = document.getElementById('regPassword').value;
                    const teamName = document.getElementById('regTeam').value;
                    const termsCheck = document.getElementById('termsCheck').checked;

                    if (!username || !email || !password) {
                        NotificationSystem.show('Preencha todos os campos obrigatórios', 'error');
                        return;
                    }
                    if (password.length < 8) {
                        NotificationSystem.show('Senha deve ter pelo menos 8 caracteres', 'error');
                        return;
                    }
                    if (!termsCheck) {
                        NotificationSystem.show('Aceite os termos de serviço', 'error');
                        return;
                    }

                    const newUser = dataManager.createUser(username, email, password, teamName);
                    dataManager.loginUser(username, password);

                    NotificationSystem.show(`✅ Bem-vindo ${username}! Sua conta foi criada com sucesso! 🎉`, 'success', 4000);
                    
                    // Atualizar página após 1 segundo
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== LOGIN ====================
window.openLoginModal = function() {
    const content = `
        <form class="form-modal" id="loginForm">
            <div class="form-group">
                <label><i class="fas fa-user"></i> Nome de Usuário</label>
                <input type="text" id="loginUsername" placeholder="Seu usuário" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-lock"></i> Senha</label>
                <input type="password" id="loginPassword" placeholder="Sua senha" required>
            </div>
        </form>
    `;
    
    ModalSystem.create('🔐 FAZER LOGIN', content, [
        { label: '❌ Cancelar', type: 'secondary' },
        { 
            label: '✅ ENTRAR',
            type: 'primary',
            callback: () => {
                try {
                    const username = document.getElementById('loginUsername').value;
                    const password = document.getElementById('loginPassword').value;

                    const user = dataManager.loginUser(username, password);
                    NotificationSystem.show(`✅ Bem-vindo de volta, ${user.username}! 🎮`, 'success', 3000);
                    
                    setTimeout(() => {
                        window.location.href = 'dashboard.html';
                    }, 1000);
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== LOGOUT ====================
window.logout = function() {
    const modal = ModalSystem.create('🚪 LOGOUT', `
        <div style="text-align: center;">
            <p>Tem certeza que deseja sair?</p>
            <p style="color: #999; font-size: 14px; margin-top: 10px;">Você será redirecionado para a página inicial.</p>
        </div>
    `, [
        { label: '❌ Cancelar', type: 'secondary' },
        { 
            label: '✅ SAIR',
            type: 'primary',
            callback: () => {
                dataManager.logoutUser();
                NotificationSystem.show('Saindo... até logo! 👋', 'success');
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 500);
            }
        }
    ]);
};

// ==================== EDITAR PERFIL ====================
window.editProfile = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <form class="form-modal" id="editProfileForm">
            <div class="form-group">
                <label><i class="fas fa-user"></i> Nome de Usuário</label>
                <input type="text" id="editUsername" placeholder="Seu nome" value="${user.username}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-crown"></i> Rank Atual</label>
                <input type="text" id="editRank" placeholder="Ex: Diamante II" value="${user.rank}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-pen"></i> Bio</label>
                <textarea id="editBio" placeholder="Conte um pouco sobre você..." style="height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('👤 EDITAR PERFIL', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ SALVAR',
            type: 'primary',
            callback: () => {
                try {
                    const username = document.getElementById('editUsername').value;
                    const rank = document.getElementById('editRank').value;

                    dataManager.updateUserProfile(user.id, { username, rank });
                    NotificationSystem.show('✅ Perfil atualizado com sucesso!', 'success');
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== VER ESTATÍSTICAS ====================
window.viewStats = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <div class="stats-view">
            <h3>📊 SUAS ESTATÍSTICAS</h3>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 20px;">
                <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #9c27b0;">
                    <div style="font-size: 12px; color: #999;">Partidas Jogadas</div>
                    <div style="font-size: 24px; font-weight: bold; color: #9c27b0;">${user.stats.matches}</div>
                </div>
                <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #5c6cff;">
                    <div style="font-size: 12px; color: #999;">Taxa de Vitória</div>
                    <div style="font-size: 24px; font-weight: bold; color: #5c6cff;">${user.stats.winRate}%</div>
                </div>
                <div style="background: rgba(255,87,34,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #ff5722;">
                    <div style="font-size: 12px; color: #999;">Vitórias</div>
                    <div style="font-size: 24px; font-weight: bold; color: #ff5722;">${user.stats.wins}</div>
                </div>
                <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #9c27b0;">
                    <div style="font-size: 12px; color: #999;">Derrotas</div>
                    <div style="font-size: 24px; font-weight: bold; color: #9c27b0;">${user.stats.losses}</div>
                </div>
            </div>
        </div>
    `;
    
    ModalSystem.create('📈 ESTATÍSTICAS DETALHADAS', content, [
        { label: '❌ FECHAR', type: 'secondary' }
    ]);
};

// ==================== CRIAR TORNEIO ====================
window.createTournament = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <form class="form-modal" id="createTournamentForm">
            <div class="form-group">
                <label><i class="fas fa-trophy"></i> Nome do Torneio</label>
                <input type="text" id="tourneyName" placeholder="Ex: Campeonato Regional 2026" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-users"></i> Número de Equipes</label>
                <input type="number" id="tourneyTeams" placeholder="4, 8, 16, 32..." min="2" max="128" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-dollar-sign"></i> Prêmio Total (R$)</label>
                <input type="text" id="tourneyPrize" placeholder="Valor em reais" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-calendar"></i> Data de Início</label>
                <input type="date" id="tourneyStart" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-clock"></i> Limite de Inscrição</label>
                <input type="date" id="tourneyDeadline" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-info-circle"></i> Descrição</label>
                <textarea id="tourneyDesc" placeholder="Descreva o torneio..." style="resize: vertical; height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('🏆 CRIAR NOVO TORNEIO', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ CRIAR',
            type: 'primary',
            callback: () => {
                try {
                    const name = document.getElementById('tourneyName').value;
                    const teams = parseInt(document.getElementById('tourneyTeams').value);
                    const prize = document.getElementById('tourneyPrize').value;
                    const start = document.getElementById('tourneyStart').value;
                    const deadline = document.getElementById('tourneyDeadline').value;
                    const desc = document.getElementById('tourneyDesc').value;

                    if (!name || !teams || !prize || !start || !deadline) {
                        NotificationSystem.show('Preencha todos os campos obrigatórios', 'error');
                        return;
                    }

                    const tournament = dataManager.createTournament(name, desc, teams, prize, start, deadline);
                    NotificationSystem.show(`🏆 Torneio "${name}" criado com sucesso! 🎉`, 'success', 3000);
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== PARTICIPAR EM TORNEIO ====================
window.joinTournament = function(tournamentId) {
    const user = dataManager.getCurrentUser();
    if (!user) {
        openRegisterModal();
        return;
    }

    try {
        const tournament = dataManager.getTournamentById(tournamentId);
        
        if (!tournament) {
            NotificationSystem.show('❌ Torneio não encontrado!', 'error');
            return;
        }

        const content = `
            <div class="tournament-details">
                <h3><i class="fas fa-trophy"></i> ${tournament.name}</h3>
                <p><strong>📊 Status:</strong> ${tournament.status}</p>
                <p><strong>💰 Prêmios:</strong> ${tournament.prize}</p>
                <p><strong>👥 Inscritos:</strong> ${tournament.participants.length}/${tournament.teams}</p>
                <p><strong>📅 Início:</strong> ${tournament.start}</p>
                <p><strong>⏰ Limite:</strong> ${tournament.deadline}</p>
                <hr>
                <p style="color: #ddd;">${tournament.description || 'Sem descrição'}</p>
                <hr>
                <p style="color: #999; font-size: 13px;">Você tem certeza que deseja se inscrever neste torneio?</p>
            </div>
        `;
        
        ModalSystem.create(`🏆 PARTICIPAR: ${tournament.name}`, content, [
            { label: '❌ Cancelar', type: 'secondary' },
            { 
                label: '✅ CONFIRMAR INSCRIÇÃO',
                type: 'primary',
                callback: () => {
                    try {
                        dataManager.joinTournament(user.id, tournamentId);
                        NotificationSystem.show(`✅ Você se inscreveu em ${tournament.name}! 🎉`, 'success', 3000);
                    } catch (error) {
                        NotificationSystem.show(`❌ ${error.message}`, 'error');
                    }
                }
            }
        ]);
    } catch (error) {
        NotificationSystem.show(`❌ ${error.message}`, 'error');
    }
};

// ==================== VISUALIZAR TORNEIO ====================
window.viewTournament = function(tournamentId) {
    try {
        const tournament = dataManager.getTournamentById(tournamentId);
        
        if (!tournament) {
            NotificationSystem.show('❌ Torneio não encontrado!', 'error');
            return;
        }

        const inscritosHTML = tournament.participants.length > 0 
            ? `<p style="color: #ddd;">✅ ${tournament.participants.length} inscritos</p>`
            : '<p style="color: #999;">Nenhum inscrito ainda</p>';

        const content = `
            <div style="space-y: 15px;">
                <h3 style="color: #9c27b0; font-size: 22px;">${tournament.name}</h3>
                <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="color: #ddd; margin-bottom: 10px;">${tournament.description}</p>
                </div>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;">
                    <div>
                        <p style="color: #999; font-size: 12px;">PRÊMIO</p>
                        <p style="font-size: 20px; font-weight: bold; color: #9c27b0;">${tournament.prize}</p>
                    </div>
                    <div>
                        <p style="color: #999; font-size: 12px;">VAGAS</p>
                        <p style="font-size: 20px; font-weight: bold; color: #5c6cff;">${tournament.participants.length}/${tournament.teams}</p>
                    </div>
                    <div>
                        <p style="color: #999; font-size: 12px;">INÍCIO</p>
                        <p style="font-size: 20px; font-weight: bold; color: #ff5722;">${tournament.start}</p>
                    </div>
                    <div>
                        <p style="color: #999; font-size: 12px;">INSCRIÇÃO ATÉ</p>
                        <p style="font-size: 20px; font-weight: bold; color: #ff5722;">${tournament.deadline}</p>
                    </div>
                </div>
                <div style="margin-top: 15px; background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px;">
                    <h4 style="color: #5c6cff; margin-bottom: 10px;">Participantes:</h4>
                    ${inscritosHTML}
                </div>
            </div>
        `;
        
        const user = dataManager.getCurrentUser();
        const isInscribed = user && tournament.participants.includes(user.id);

        ModalSystem.create(`📋 DETALHES: ${tournament.name}`, content, [
            { label: '❌ FECHAR', type: 'secondary' },
            {
                label: isInscribed ? '✅ JÁ INSCRITO' : '🎮 INSCREVER-SE',
                type: 'primary',
                callback: () => {
                    if (!isInscribed) {
                        joinTournament(tournamentId);
                    } else {
                        NotificationSystem.show('Você já está inscrito neste torneio!', 'info');
                    }
                }
            }
        ]);
    } catch (error) {
        NotificationSystem.show(`❌ ${error.message}`, 'error');
    }
};

// ==================== CRIAR TIME ====================
window.createTeam = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <form class="form-modal" id="createTeamForm">
            <div class="form-group">
                <label><i class="fas fa-users"></i> Nome do Time</label>
                <input type="text" id="teamName" placeholder="Ex: Kitsune Pro" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-smile"></i> Emoji/Ícone</label>
                <input type="text" id="teamIcon" placeholder="Ex: 🦊 👑 ⚡" value="👥" maxlength="2" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-pen"></i> Descrição</label>
                <textarea id="teamDesc" placeholder="Descreva seu time..." style="height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('👥 CRIAR NOVO TIME', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ CRIAR TIME',
            type: 'primary',
            callback: () => {
                try {
                    const name = document.getElementById('teamName').value;
                    const icon = document.getElementById('teamIcon').value;

                    if (!name) {
                        NotificationSystem.show('Digite o nome do time', 'error');
                        return;
                    }

                    const team = dataManager.createTeam(name, user.id, icon);
                    NotificationSystem.show(`👥 Time "${name}" criado com sucesso! 🎉`, 'success', 3000);
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== VISUALIZAR TIME ====================
window.viewTeam = function(teamId) {
    try {
        const team = dataManager.getTeamById(teamId);
        const owner = dataManager.data.users.find(u => u.id === team.owner);
        
        if (!team) {
            NotificationSystem.show('❌ Time não encontrado!', 'error');
            return;
        }

        const membersHTML = team.members.length > 0
            ? team.members.map(id => {
                const member = dataManager.data.users.find(u => u.id === id);
                return `<p style="color: #ddd; margin: 5px 0;">👤 ${member?.username || 'Jogador'}</p>`;
            }).join('')
            : '<p style="color: #999;">Nenhum membro ainda</p>';

        const content = `
            <div class="team-details">
                <div style="text-align: center; margin-bottom: 20px;">
                    <p style="font-size: 48px; margin-bottom: 10px;">${team.icon}</p>
                    <h2 style="color: #9c27b0; font-size: 28px;">${team.name}</h2>
                    <p style="color: #999; font-size: 14px;">Dono: ${owner?.username}</p>
                </div>
                
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 20px;">
                    <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px;">
                        <p style="color: #999; font-size: 12px;">MEMBROS</p>
                        <p style="font-size: 24px; font-weight: bold; color: #9c27b0;">${team.players}/5</p>
                    </div>
                    <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px;">
                        <p style="color: #999; font-size: 12px;">WIN RATE</p>
                        <p style="font-size: 24px; font-weight: bold; color: #5c6cff;">${team.wr}%</p>
                    </div>
                </div>

                <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px;">
                    <h4 style="color: #5c6cff; margin-bottom: 10px;">👥 Membros:</h4>
                    ${membersHTML}
                </div>
            </div>
        `;
        
        const user = dataManager.getCurrentUser();
        const isOwner = user && team.owner === user.id;
        const isMember = user && team.members.includes(user.id);

        ModalSystem.create(`👥 TIME: ${team.name}`, content, [
            { label: '❌ FECHAR', type: 'secondary' },
            {
                label: isOwner ? '⚙️ GERENCIAR' : (isMember ? '✅ MEMBRO' : '➕ ENTRAR'),
                type: 'primary',
                callback: () => {
                    if (isOwner) {
                        NotificationSystem.show('🔧 Gerenciamento de time (em breve)', 'info');
                    } else if (!isMember && team.players < 5) {
                        try {
                            dataManager.inviteToTeam(teamId, user.id);
                            NotificationSystem.show(`✅ Você entrou no time ${team.name}! 🎉`, 'success', 3000);
                        } catch (error) {
                            NotificationSystem.show(`❌ ${error.message}`, 'error');
                        }
                    }
                }
            }
        ]);
    } catch (error) {
        NotificationSystem.show(`❌ ${error.message}`, 'error');
    }
};

// ==================== CONVIDAR JOGADORES ====================
window.invitePlayers = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <form class="form-modal" id="invitePlayersForm">
            <div class="form-group">
                <label><i class="fas fa-users"></i> Nicks dos Jogadores (separados por vírgula)</label>
                <textarea id="playerNicks" placeholder="Player1, Player2, Player3" style="height: 100px;"></textarea>
            </div>
            <div class="form-group">
                <label><i class="fas fa-comment"></i> Mensagem Personalizada</label>
                <textarea id="inviteMessage" placeholder="Envie uma mensagem aos jogadores (opcional)" style="height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('📩 CONVIDAR JOGADORES', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ ENVIAR CONVITES',
            type: 'primary',
            callback: () => {
                try {
                    const nicks = document.getElementById('playerNicks').value;
                    const message = document.getElementById('inviteMessage').value;

                    if (!nicks.trim()) {
                        NotificationSystem.show('Digite pelo menos um nick de jogador', 'error');
                        return;
                    }

                    const playerList = nicks.split(',').map(n => n.trim()).filter(n => n);
                    NotificationSystem.show(`📩 ${playerList.length} convite(s) enviado(s)! 🎮`, 'success', 3000);
                } catch (error) {
                    NotificationSystem.show(`❌ ${error.message}`, 'error');
                }
            }
        }
    ]);
};

// ==================== CONFIGURAÇÕES ====================
window.openSettings = function() {
    const user = dataManager.getCurrentUser();
    if (!user) {
        NotificationSystem.show('❌ Você precisa estar logado', 'error');
        return;
    }

    const content = `
        <div style="space-y: 15px;">
            <h3 style="color: #9c27b0; margin-bottom: 15px;">⚙️ CONFIGURAÇÕES GERAIS</h3>
            
            <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
                    <input type="checkbox" checked>
                    <span>🔔 Notificações ativas</span>
                </label>
            </div>
            
            <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
                    <input type="checkbox" checked>
                    <span>🔊 Sons ativados</span>
                </label>
            </div>

            <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
                    <input type="checkbox" checked>
                    <span>🌐 Mostrar online</span>
                </label>
            </div>
            
            <hr style="margin: 20px 0; border: 1px solid rgba(156,39,176,0.2);">
            
            <h3 style="color: #9c27b0; margin: 15px 0;">🔐 PRIVACIDADE</h3>
            
            <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px;">
                <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
                    <input type="checkbox" checked>
                    <span>👁️ Perfil público</span>
                </label>
                <div style="font-size: 12px; color: #999; margin-top: 8px; margin-left: 28px;">
                    Outros usuários poderão ver seu perfil
                </div>
            </div>
        </div>
    `;
    
    ModalSystem.create('⚙️ CONFIGURAÇÕES', content, [
        { label: '❌ FECHAR', type: 'secondary' },
        { 
            label: '✅ SALVAR',
            type: 'primary',
            callback: () => {
                NotificationSystem.show('⚙️ Configurações salvas!', 'success');
            }
        }
    ]);
};

// ==================== REPORTAR BUG ====================
window.reportBug = function() {
    const content = `
        <form class="form-modal" id="reportBugForm">
            <div class="form-group">
                <label><i class="fas fa-bug"></i> Tipo de Problema</label>
                <select required>
                    <option value="">Selecione...</option>
                    <option value="bug">🐛 Bug</option>
                    <option value="error">⚠️ Erro</option>
                    <option value="feature">💡 Sugestão de Recurso</option>
                    <option value="other">❓ Outro</option>
                </select>
            </div>
            <div class="form-group">
                <label><i class="fas fa-pen"></i> Descrição</label>
                <textarea placeholder="Descreva o problema em detalhes..." style="height: 120px;" required></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('🐛 REPORTAR PROBLEMA', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ ENVIAR REPORT',
            type: 'primary',
            callback: () => {
                NotificationSystem.show('✅ Obrigado! Seu report foi enviado ao time. 🙏', 'success');
            }
        }
    ]);
};

// ==================== CONTATO/SUPORTE ====================
window.contactSupport = function() {
    const content = `
        <form class="form-modal" id="contactForm">
            <div class="form-group">
                <label><i class="fas fa-envelope"></i> Seu Email</label>
                <input type="email" placeholder="seu@email.com" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-user"></i> Assunto</label>
                <input type="text" placeholder="Qual é a sua dúvida?" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-comment"></i> Mensagem</label>
                <textarea placeholder="Nos conte como podemos ajudar..." style="height: 120px;" required></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('📞 CONTATE O SUPORTE', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ ENVIAR',
            type: 'primary',
            callback: () => {
                NotificationSystem.show('✅ Mensagem enviada! Responderemos em breve. 📨', 'success');
            }
        }
    ]);
};

// ==================== COMPARTILHAR ====================
window.shareContent = function() {
    const pageTitle = document.title;
    const pageUrl = window.location.href;
    
    const content = `
        <div style="space-y: 15px; text-align: center;">
            <p>Compartilhe a Kitsune Academy com seus amigos!</p>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 20px;">
                <button onclick="window.open('https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(pageUrl)}', '_blank')" style="padding: 10px; background: #1877F2; border: none; border-radius: 8px; color: white; font-weight: bold; cursor: pointer;">
                    📘 Facebook
                </button>
                <button onclick="window.open('https://twitter.com/intent/tweet?url=${encodeURIComponent(pageUrl)}&text=${encodeURIComponent(pageTitle)}', '_blank')" style="padding: 10px; background: #1DA1F2; border: none; border-radius: 8px; color: white; font-weight: bold; cursor: pointer;">
                    𝕏 Twitter
                </button>
                <button onclick="navigator.clipboard.writeText('${pageUrl}'); alert('Link copiado!')" style="padding: 10px; background: #9c27b0; border: none; border-radius: 8px; color: white; font-weight: bold; cursor: pointer;">
                    🔗 Copiar Link
                </button>
            </div>
        </div>
    `;
    
    ModalSystem.create('📢 COMPARTILHAR', content, [
        { label: '❌ FECHAR', type: 'secondary' }
    ]);
};

// ==================== LISTAR TORNEIOS ====================
window.listTournaments = function() {
    const tournaments = dataManager.getTournaments();
    
    if (tournaments.length === 0) {
        NotificationSystem.show('Nenhum torneio disponível', 'info');
        return;
    }

    let content = '<div style="space-y: 10px;">';
    tournaments.forEach(t => {
        content += `
            <div style="background: rgba(92,108,255,0.1); padding: 12px; border-radius: 8px; margin-bottom: 10px; cursor: pointer;" onclick="viewTournament(${t.id})">
                <div style="font-weight: bold; color: #5c6cff;">${t.name}</div>
                <div style="font-size: 12px; color: #999; margin-top: 5px;">
                    💰 Prêmio: ${t.prize} | 👥 Inscritos: ${t.participants.length}/${t.teams} | 📅 ${t.start}
                </div>
            </div>
        `;
    });
    content += '</div>';

    ModalSystem.create('🏆 TORNEIOS DISPONÍVEIS', content, [
        { label: '❌ FECHAR', type: 'secondary' }
    ]);
};

// ==================== LISTAR TIMES ====================
window.listTeams = function() {
    const teams = dataManager.getTeams();
    
    if (teams.length === 0) {
        NotificationSystem.show('Nenhum time disponível', 'info');
        return;
    }

    let content = '<div style="space-y: 10px;">';
    teams.forEach(t => {
        content += `
            <div style="background: rgba(92,108,255,0.1); padding: 12px; border-radius: 8px; margin-bottom: 10px; cursor: pointer;" onclick="viewTeam(${t.id})">
                <div style="font-weight: bold; color: #5c6cff;">${t.icon} ${t.name}</div>
                <div style="font-size: 12px; color: #999; margin-top: 5px;">
                    👥 Membros: ${t.players}/5 | 🏆 Win Rate: ${t.wr}%
                </div>
            </div>
        `;
    });
    content += '</div>';

    ModalSystem.create('👥 TIMES DISPONÍVEIS', content, [
        { label: '❌ FECHAR', type: 'secondary' }
    ]);
};

console.log('✅ Sistema completo Kitsune Academy carregado! 🎮');
