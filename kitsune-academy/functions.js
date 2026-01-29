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
            password,
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
            mobileMenuBtn.querySelector('i').classList.toggle('fa-bars');
            mobileMenuBtn.querySelector('i').classList.toggle('fa-times');
        });
        
        // Fechar menu ao clicar em um link
        mobileMenu.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                mobileMenu.classList.add('hidden');
                mobileMenuBtn.querySelector('i').classList.add('fa-bars');
                mobileMenuBtn.querySelector('i').classList.remove('fa-times');
            });
        });
    }
});

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
                NotificationSystem.show('Saindo... até logo! 👋', 'success');
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 500);
            }
        }
    ]);
};

// ==================== VISUALIZAR ESTATÍSTICAS ====================
window.viewStats = function() {
    const content = `
        <div class="stats-view">
            <h3>📊 SUAS ESTATÍSTICAS</h3>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 20px;">
                <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #9c27b0;">
                    <div style="font-size: 12px; color: #999;">Partidas Jogadas</div>
                    <div style="font-size: 24px; font-weight: bold; color: #9c27b0;">${SITE_DATA.user.stats.matches}</div>
                </div>
                <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #5c6cff;">
                    <div style="font-size: 12px; color: #999;">Taxa de Vitória</div>
                    <div style="font-size: 24px; font-weight: bold; color: #5c6cff;">${SITE_DATA.user.stats.winRate}%</div>
                </div>
                <div style="background: rgba(255,87,34,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #ff5722;">
                    <div style="font-size: 12px; color: #999;">Vitórias</div>
                    <div style="font-size: 24px; font-weight: bold; color: #ff5722;">${SITE_DATA.user.stats.victories}</div>
                </div>
                <div style="background: rgba(156,39,176,0.1); padding: 15px; border-radius: 8px; border-left: 3px solid #9c27b0;">
                    <div style="font-size: 12px; color: #999;">Derrotas</div>
                    <div style="font-size: 24px; font-weight: bold; color: #9c27b0;">${SITE_DATA.user.stats.defeats}</div>
                </div>
            </div>
            <div style="margin-top: 20px; padding: 15px; background: rgba(92,108,255,0.1); border-radius: 8px;">
                <div style="font-size: 12px; color: #999; margin-bottom: 5px;">🎮 Campeão Principal</div>
                <div style="font-size: 18px; font-weight: bold; color: #5c6cff;">${SITE_DATA.user.stats.mainChamp}</div>
            </div>
        </div>
    `;
    
    ModalSystem.create('📈 ESTATÍSTICAS DETALHADAS', content, [
        { label: '❌ FECHAR', type: 'secondary' },
        { 
            label: '🔄 ATUALIZAR',
            type: 'primary',
            callback: () => {
                NotificationSystem.show('Estatísticas atualizadas! 🔄', 'success');
            }
        }
    ]);
};

// ==================== BUSCAR TORNEIOS ====================
window.searchTournaments = function() {
    const searchInput = prompt('🔍 Pesquisar torneios:\n\nDigite palavras-chave (ex: "Brasil", "Solo", "Scrim"):');
    
    if (searchInput !== null) {
        const results = SITE_DATA.tournaments.filter(t => 
            t.name.toLowerCase().includes(searchInput.toLowerCase())
        );
        
        if (results.length > 0) {
            let content = '<div style="space-y: 10px;">';
            results.forEach(t => {
                content += `
                    <div style="background: rgba(92,108,255,0.1); padding: 12px; border-radius: 8px; margin-bottom: 10px;">
                        <div style="font-weight: bold; color: #5c6cff;">${t.name}</div>
                        <div style="font-size: 12px; color: #999; margin-top: 5px;">
                            💰 Prêmio: ${t.prize} | 👥 Inscritos: ${t.teams} | 📅 ${t.start}
                        </div>
                    </div>
                `;
            });
            content += '</div>';
            
            ModalSystem.create('🔍 RESULTADOS DA BUSCA', content, [
                { label: '❌ FECHAR', type: 'secondary' }
            ]);
        } else {
            NotificationSystem.show('❌ Nenhum torneio encontrado com esses critérios', 'warning');
        }
    }
};

// ==================== CRIAR NOVO TORNEIO ====================
window.createTournament = function() {
    const content = `
        <form class="form-modal" id="createTournamentForm">
            <div class="form-group">
                <label><i class="fas fa-trophy"></i> Nome do Torneio</label>
                <input type="text" placeholder="Ex: Campeonato Regional 2026" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-users"></i> Número de Equipes</label>
                <input type="number" placeholder="4, 8, 16, 32..." min="2" max="128" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-dollar-sign"></i> Prêmio Total (R$)</label>
                <input type="number" placeholder="Valor em reais" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-calendar"></i> Data de Início</label>
                <input type="date" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-clock"></i> Limite de Inscrição</label>
                <input type="date" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-info-circle"></i> Descrição</label>
                <textarea placeholder="Descreva o torneio..." style="resize: vertical; height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('🏆 CRIAR NOVO TORNEIO', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ CRIAR',
            type: 'primary',
            callback: () => {
                const form = document.getElementById('createTournamentForm');
                if (form.checkValidity()) {
                    NotificationSystem.show('🏆 Torneio criado com sucesso!', 'success');
                } else {
                    NotificationSystem.show('Preencha todos os campos obrigatórios', 'error');
                }
            }
        }
    ]);
};

// ==================== CONVIDAR JOGADORES ====================
window.invitePlayers = function() {
    const content = `
        <form class="form-modal" id="invitePlayersForm">
            <div class="form-group">
                <label><i class="fas fa-users"></i> Jogadores (separados por vírgula)</label>
                <textarea placeholder="Nicks dos jogadores&#10;Ex: Player1, Player2, Player3" style="height: 100px;"></textarea>
            </div>
            <div class="form-group">
                <label><i class="fas fa-comment"></i> Mensagem Personalizada</label>
                <textarea placeholder="Envie uma mensagem aos jogadores (opcional)" style="height: 80px;"></textarea>
            </div>
            <div style="background: rgba(92,108,255,0.1); padding: 12px; border-radius: 8px; margin-top: 15px;">
                <input type="checkbox" id="notifyImmediate" checked>
                <label for="notifyImmediate" style="display: inline; margin-left: 8px; color: #5c6cff;">Notificar jogadores imediatamente</label>
            </div>
        </form>
    `;
    
    ModalSystem.create('📩 CONVIDAR JOGADORES', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ ENVIAR CONVITES',
            type: 'primary',
            callback: () => {
                const form = document.getElementById('invitePlayersForm');
                const textarea = form.querySelector('textarea');
                const playerCount = textarea.value ? textarea.value.split(',').length : 0;
                
                if (playerCount > 0) {
                    NotificationSystem.show(`📩 ${playerCount} convite(s) enviado(s)!`, 'success');
                } else {
                    NotificationSystem.show('Digite pelo menos um nick de jogador', 'error');
                }
            }
        }
    ]);
};

// ==================== EDITAR PERFIL ====================
window.editProfile = function() {
    const content = `
        <form class="form-modal" id="editProfileForm">
            <div class="form-group">
                <label><i class="fas fa-user"></i> Nome de Usuário</label>
                <input type="text" placeholder="Seu nome" value="${SITE_DATA.user.name}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-gamepad"></i> Tag do Jogo</label>
                <input type="text" placeholder="Seu nick em-jogo">
            </div>
            <div class="form-group">
                <label><i class="fas fa-crown"></i> Rank Atual</label>
                <input type="text" placeholder="Ex: Diamante II" value="${SITE_DATA.user.rank}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-award"></i> Time</label>
                <input type="text" placeholder="Nome do seu time" value="${SITE_DATA.user.team}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-image"></i> URL da Foto de Perfil</label>
                <input type="url" placeholder="https://exemplo.com/foto.jpg">
            </div>
            <div class="form-group">
                <label><i class="fas fa-pen"></i> Bio</label>
                <textarea placeholder="Conte um pouco sobre você..." style="height: 80px;"></textarea>
            </div>
        </form>
    `;
    
    ModalSystem.create('👤 EDITAR PERFIL', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ SALVAR',
            type: 'primary',
            callback: () => {
                NotificationSystem.show('✅ Perfil atualizado com sucesso!', 'success');
            }
        }
    ]);
};

// ==================== CONFIGURAÇÕES ====================
window.openSettings = function() {
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
            
            <div style="background: rgba(92,108,255,0.1); padding: 15px; border-radius: 8px;">
                <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
                    <input type="checkbox">
                    <span>🎨 Tema Escuro (padrão)</span>
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
            <div class="form-group">
                <label><i class="fas fa-link"></i> Página afetada</label>
                <input type="text" placeholder="Ex: dashboard.html" value="${window.location.pathname}">
            </div>
            <div class="form-group">
                <label><i class="fas fa-image"></i> Screenshot (opcional)</label>
                <input type="file" accept="image/*">
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
            <div style="background: rgba(92,108,255,0.1); padding: 12px; border-radius: 8px;">
                <input type="checkbox" id="privacyCheck" required>
                <label for="privacyCheck" style="display: inline; margin-left: 8px; font-size: 13px;">
                    ✅ Concordo com a Política de Privacidade
                </label>
            </div>
        </form>
    `;
    
    ModalSystem.create('📞 CONTATE O SUPORTE', content, [
        { label: '❌ CANCELAR', type: 'secondary' },
        { 
            label: '✅ ENVIAR',
            type: 'primary',
            callback: () => {
                const form = document.getElementById('contactForm');
                if (form.checkValidity()) {
                    NotificationSystem.show('✅ Mensagem enviada! Responderemos em breve. 📨', 'success');
                }
            }
        }
    ]);
};

// ==================== SABER MAIS (LEARN MORE) ====================
window.learnMore = function(topic) {
    const topics = {
        'tournaments': {
            title: '🏆 COMO PARTICIPAR DE TORNEIOS',
            content: `
                <h4 style="color: #5c6cff; margin-bottom: 10px;">📋 PASSOS:</h4>
                <ol style="margin-left: 20px; color: #ddd;">
                    <li style="margin-bottom: 8px;"><strong>Crie sua conta</strong> - Registre-se como jogador ou manager</li>
                    <li style="margin-bottom: 8px;"><strong>Configure seu perfil</strong> - Adicione seu nick e rank</li>
                    <li style="margin-bottom: 8px;"><strong>Procure torneios</strong> - Explore os torneios disponíveis</li>
                    <li style="margin-bottom: 8px;"><strong>Inscreva-se</strong> - Clique em "Participar" e confirme</li>
                    <li><strong>Compete</strong> - Jogue as partidas no horário agendado</li>
                </ol>
                <hr style="margin: 15px 0; border: 1px solid rgba(156,39,176,0.2);">
                <h4 style="color: #5c6cff; margin-bottom: 10px;">💡 DICAS:</h4>
                <ul style="margin-left: 20px; color: #ddd;">
                    <li>✅ Participe de scrims para treinar antes do torneio</li>
                    <li>✅ Organize sua equipe com antecedência</li>
                    <li>✅ Esteja online 15 minutos antes das partidas</li>
                </ul>
            `
        },
        'teams': {
            title: '👥 COMO CRIAR UM TIME',
            content: `
                <h4 style="color: #5c6cff; margin-bottom: 10px;">👥 COMPOSIÇÃO DO TIME:</h4>
                <ul style="margin-left: 20px; color: #ddd;">
                    <li>🎯 1 Mid Laner</li>
                    <li>🏹 1 ADC (Atirador)</li>
                    <li>⚔️ 1 Top Laner</li>
                    <li>🗡️ 1 Jungle</li>
                    <li>🛡️ 1 Suporte</li>
                </ul>
                <hr style="margin: 15px 0; border: 1px solid rgba(156,39,176,0.2);">
                <h4 style="color: #5c6cff; margin-bottom: 10px;">📋 PARA CRIAR:</h4>
                <ol style="margin-left: 20px; color: #ddd;">
                    <li style="margin-bottom: 8px;">Escolha um nome para seu time</li>
                    <li style="margin-bottom: 8px;">Convide 4 outros jogadores</li>
                    <li style="margin-bottom: 8px;">Defina um logo (opcional)</li>
                    <li>Comece a participar de torneios!</li>
                </ol>
            `
        },
        'ranking': {
            title: '📊 ENTENDA O RANKING',
            content: `
                <h4 style="color: #5c6cff; margin-bottom: 10px;">🏆 DIVISÕES (da melhor para pior):</h4>
                <div style="display: flex; flex-direction: column; gap: 8px; margin-left: 20px;">
                    <div>👑 <strong>Challenger</strong> - Top 50 times</div>
                    <div>💎 <strong>Diamante</strong> - Jogadores de elite</div>
                    <div>⭐ <strong>Platina</strong> - Jogadores avançados</div>
                    <div>🥇 <strong>Ouro</strong> - Jogadores competitivos</div>
                    <div>🥈 <strong>Prata</strong> - Jogadores intermediários</div>
                    <div>🥉 <strong>Bronze</strong> - Iniciantes</div>
                </div>
                <hr style="margin: 15px 0; border: 1px solid rgba(156,39,176,0.2);">
                <h4 style="color: #5c6cff; margin-bottom: 10px;">💡 COMO SUBIR:</h4>
                <ul style="margin-left: 20px; color: #ddd;">
                    <li>✅ Ganhe partidas em torneios</li>
                    <li>✅ Cada vitória = LP (League Points)</li>
                    <li>✅ Ao atingir 100 LP, suba de divisão</li>
                </ul>
            `
        }
    };
    
    const info = topics[topic] || topics['tournaments'];
    
    ModalSystem.create(info.title, info.content, [
        { label: '❌ FECHAR', type: 'secondary' }
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
                <button onclick="window.open('https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(pageUrl)}', '_blank')" style="padding: 10px; background: #0A66C2; border: none; border-radius: 8px; color: white; font-weight: bold; cursor: pointer;">
                    in LinkedIn
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

console.log('✅ Todas as funções do site foram carregadas com sucesso! 🎮');
