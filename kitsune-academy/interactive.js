/**
 * ============================================================
 * KITSUNE ACADEMY - SISTEMA INTERATIVO COMPLETO
 * ============================================================
 * Todas as funcionalidades dinâmicas do site
 */

// ==================== DADOS GLOBAIS ====================
const SITE_DATA = {
    user: {
        name: 'Igor Parente',
        rank: 'Diamante II',
        lp: 1245,
        team: 'Kitsune Legends',
        teamRank: 12,
        stats: {
            matches: 127,
            winRate: 62,
            mainChamp: 'Ahri',
            victories: 79,
            defeats: 48
        }
    },
    tournaments: [
        {
            id: 1,
            name: 'Campeonato Brasileiro 2026',
            teams: 32,
            prize: 'R$ 50.000',
            status: 'active',
            progress: 75,
            start: '15 de Fevereiro',
            deadline: '10 de Fevereiro'
        },
        {
            id: 2,
            name: 'Torneio Solo Ranking 2026',
            teams: 256,
            prize: 'R$ 20.000',
            status: 'running',
            progress: 45,
            start: 'Qualificatórias',
            deadline: '28 de Fevereiro'
        },
        {
            id: 3,
            name: 'Scrim League - Treinamento',
            teams: 16,
            prize: 'Sem Prêmios',
            status: 'upcoming',
            progress: 0,
            start: '01 de Março',
            deadline: '25 de Fevereiro'
        }
    ],
    teams: [
        { name: 'KSN Esports', rank: 1, wins: '15-2', wr: 94, players: 5, icon: '👑' },
        { name: 'ProFire Gaming', rank: 2, wins: '13-3', wr: 81, players: 5, icon: '🔥' },
        { name: 'Alpha Legends', rank: 3, wins: '12-4', wr: 75, players: 5, icon: '⭐' },
        { name: 'Elite Squad', rank: 4, wins: '10-5', wr: 66, players: 5, icon: '🚀' },
        { name: 'Apex Titans', rank: 5, wins: '9-6', wr: 60, players: 5, icon: '🔥' },
        { name: 'Shadow Legends', rank: 6, wins: '8-7', wr: 53, players: 5, icon: '🏆' }
    ]
};

// ==================== UTILITÁRIOS ====================
class NotificationSystem {
    static show(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <i class="fas fa-${this.getIcon(type)}"></i>
            <span>${message}</span>
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => notification.classList.add('show'), 10);
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, duration);
    }
    
    static getIcon(type) {
        const icons = {
            'success': 'check-circle',
            'error': 'exclamation-circle',
            'warning': 'exclamation-triangle',
            'info': 'info-circle'
        };
        return icons[type] || 'info-circle';
    }
}

// ==================== MODAL SYSTEM ====================
class ModalSystem {
    static create(title, content, actions = []) {
        const modal = document.createElement('div');
        modal.className = 'modal-overlay';
        modal.innerHTML = `
            <div class="modal">
                <div class="modal-header">
                    <h2><i class="fas fa-lock"></i> ${title}</h2>
                    <button class="modal-close"><i class="fas fa-times"></i></button>
                </div>
                <div class="modal-body">
                    ${content}
                </div>
                <div class="modal-footer">
                    ${actions.map((action, idx) => `
                        <button class="btn-modal ${action.type || 'secondary'}" data-action="${idx}">
                            ${action.label}
                        </button>
                    `).join('')}
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Fechar modal
        modal.querySelector('.modal-close').onclick = () => {
            modal.classList.remove('show');
            setTimeout(() => modal.remove(), 300);
        };
        
        modal.onclick = (e) => {
            if (e.target === modal) {
                modal.classList.remove('show');
                setTimeout(() => modal.remove(), 300);
            }
        };
        
        // Ações
        actions.forEach((action, idx) => {
            const btn = modal.querySelector(`[data-action="${idx}"]`);
            if (btn) {
                btn.onclick = () => {
                    if (action.callback) action.callback();
                    modal.classList.remove('show');
                    setTimeout(() => modal.remove(), 300);
                };
            }
        });
        
        // Trigger animation
        setTimeout(() => modal.classList.add('show'), 10);
        return modal;
    }
}

// ==================== REGISTRO DE USUÁRIO ====================
function openRegisterModal() {
    const content = `
        <form class="form-modal" id="registerForm">
            <div class="form-group">
                <label><i class="fas fa-user"></i> Nome de Usuário</label>
                <input type="text" placeholder="Seu nick no jogo" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-envelope"></i> Email</label>
                <input type="email" placeholder="seu@email.com" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-lock"></i> Senha</label>
                <input type="password" placeholder="Mínimo 8 caracteres" required>
            </div>
            <div class="form-group">
                <label><i class="fas fa-shield-alt"></i> Time (Opcional)</label>
                <input type="text" placeholder="Nome do seu time">
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
                const form = document.getElementById('registerForm');
                if (form.checkValidity()) {
                    NotificationSystem.show('Conta criada com sucesso! 🎉 Bem-vindo à Kitsune Academy!', 'success');
                } else {
                    NotificationSystem.show('Preencha todos os campos obrigatórios', 'error');
                }
            }
        }
    ]);
}

// ==================== PARTICIPAR EM TORNEIO ====================
function joinTournament(tournamentId) {
    const tournament = SITE_DATA.tournaments.find(t => t.id === tournamentId);
    
    const content = `
        <div class="tournament-details">
            <h3><i class="fas fa-trophy"></i> ${tournament.name}</h3>
            <p><strong>📊 Status:</strong> ${tournament.status}</p>
            <p><strong>💰 Prêmios:</strong> ${tournament.prize}</p>
            <p><strong>👥 Inscritos:</strong> ${tournament.teams} ${tournament.status === 'running' ? 'jogadores' : 'equipes'}</p>
            <p><strong>📅 Início:</strong> ${tournament.start}</p>
            <hr>
            <p>Você tem certeza que deseja se inscrever neste torneio?</p>
        </div>
    `;
    
    ModalSystem.create(`🏆 PARTICIPAR: ${tournament.name}`, content, [
        { label: '❌ Cancelar', type: 'secondary' },
        { 
            label: '✅ CONFIRMAR INSCRIÇÃO',
            type: 'primary',
            callback: () => {
                NotificationSystem.show(`✅ Você se inscreveu em ${tournament.name}! 🎉`, 'success');
            }
        }
    ]);
}

// ==================== VISUALIZAR TIME ====================
function viewTeam(teamName) {
    const team = SITE_DATA.teams.find(t => t.name === teamName);
    
    const content = `
        <div class="team-details">
            <div class="team-header">
                <h2>${team.icon} ${team.name}</h2>
                <span class="rank-badge">🏆 Rank #${team.rank}</span>
            </div>
            <div class="team-stats-detailed">
                <div class="stat-item">
                    <i class="fas fa-trophy"></i>
                    <span><strong>${team.wins}</strong> Vitórias</span>
                </div>
                <div class="stat-item">
                    <i class="fas fa-percent"></i>
                    <span><strong>${team.wr}%</strong> Win Rate</span>
                </div>
                <div class="stat-item">
                    <i class="fas fa-users"></i>
                    <span><strong>${team.players}</strong> Jogadores</span>
                </div>
            </div>
            <div class="team-roster">
                <h4>👥 Escalação</h4>
                <ul>
                    <li>🎯 Mid - Lux Main (Ahri, Syndra, Lux)</li>
                    <li>🏹 ADC - CarryGod (Xayah, Jhin, Aphelios)</li>
                    <li>⚔️ Top - TankMaster (Darius, Sion, Malphite)</li>
                    <li>🗡️ Jungle - GankKing (Lee Sin, Graves, Nidalee)</li>
                    <li>🛡️ Support - Engage (Thresh, Blitzcrank, Nautilus)</li>
                </ul>
            </div>
        </div>
    `;
    
    ModalSystem.create(`👥 TIME: ${team.name}`, content, [
        { label: '❌ FECHAR', type: 'secondary' },
        { 
            label: '⚔️ DESAFIAR TIME',
            type: 'primary',
            callback: () => {
                NotificationSystem.show(`🎮 Desafio enviado para ${team.name}!`, 'success');
            }
        }
    ]);
}

// ==================== ANIMAÇÕES DE SCROLL ====================
class ScrollAnimations {
    static init() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animated');
                }
            });
        }, { threshold: 0.1 });
        
        document.querySelectorAll('.reveal, .feature-card, .tournament-card, .team-card, .dashboard-card, .video-card').forEach(el => {
            observer.observe(el);
        });
    }
}

// ==================== FILTROS DINÂMICOS ====================
class FilterSystem {
    static filterTournaments(status) {
        const cards = document.querySelectorAll('.tournament-card');
        cards.forEach(card => {
            const cardStatus = card.querySelector('.badge')?.className.includes('active') ? 'active' :
                              card.querySelector('.badge')?.className.includes('running') ? 'running' : 'upcoming';
            
            card.style.opacity = status === 'all' || cardStatus === status ? '1' : '0.3';
            card.style.pointerEvents = status === 'all' || cardStatus === status ? 'auto' : 'none';
        });
    }
    
    static searchTeams(query) {
        const cards = document.querySelectorAll('.team-card');
        cards.forEach(card => {
            const teamName = card.querySelector('h3')?.textContent || '';
            const matches = teamName.toLowerCase().includes(query.toLowerCase());
            card.style.display = matches ? 'block' : 'none';
        });
    }
    
    static sortTeams(sortBy) {
        const grid = document.querySelector('.teams-grid');
        if (!grid) return;
        
        const cards = Array.from(document.querySelectorAll('.team-card'));
        
        if (sortBy === 'rank') {
            cards.sort((a, b) => {
                const rankA = parseInt(a.querySelector('.rank')?.textContent) || 0;
                const rankB = parseInt(b.querySelector('.rank')?.textContent) || 0;
                return rankA - rankB;
            });
        } else if (sortBy === 'wr') {
            cards.sort((a, b) => {
                const wrA = parseFloat(a.querySelector('.stat-value')?.textContent) || 0;
                const wrB = parseFloat(b.querySelector('.stat-value')?.textContent) || 0;
                return wrB - wrA;
            });
        }
        
        cards.forEach(card => grid.appendChild(card));
        NotificationSystem.show(`Ordenado por ${sortBy === 'rank' ? 'Ranking' : 'Win Rate'}`, 'info');
    }
}

// ==================== CONTADOR EM TEMPO REAL ====================
class RealtimeCounters {
    static init() {
        // Contador de usuários online
        const onlineCounter = document.querySelector('[data-online-users]');
        if (onlineCounter) {
            let count = 1250;
            setInterval(() => {
                count += Math.floor(Math.random() * 5) - 1;
                onlineCounter.textContent = count.toLocaleString();
            }, 3000);
        }
        
        // Contador de torneios ativos
        const activeTournaments = document.querySelector('[data-active-tournaments]');
        if (activeTournaments) {
            let count = 47;
            setInterval(() => {
                if (Math.random() > 0.7) count++;
                activeTournaments.textContent = count;
            }, 5000);
        }
    }
}

// ==================== PROGRESSO DE INSCRIÇÃO ====================
class ProgressAnimator {
    static animateProgress(element, targetValue, duration = 1000) {
        const progressFill = element.querySelector('.progress-fill');
        if (!progressFill) return;
        
        const startValue = 0;
        const startTime = Date.now();
        
        const animate = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const currentValue = startValue + (targetValue - startValue) * progress;
            
            progressFill.style.width = currentValue + '%';
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };
        
        animate();
    }
}

// ==================== HOVER EFFECTS AVANÇADOS ====================
class AdvancedHoverEffects {
    static init() {
        document.querySelectorAll('.feature-card, .tournament-card, .team-card, .video-card').forEach(card => {
            card.addEventListener('mouseenter', () => {
                card.style.transform = 'translateY(-15px) scale(1.02)';
                card.style.boxShadow = '0 30px 70px rgba(0,0,0,0.7), 0 0 40px rgba(156,39,176,0.6)';
            });
            
            card.addEventListener('mouseleave', () => {
                card.style.transform = '';
                card.style.boxShadow = '';
            });
        });
    }
}

// ==================== SISTEMA DE NOTIFICAÇÕES ====================
class LiveNotifications {
    static init() {
        const notifications = [
            { msg: 'Nova partida disponível!', type: 'info', delay: 5000 },
            { msg: 'Time convidou você para Scrim!', type: 'success', delay: 15000 },
            { msg: 'Torneio começará em 1 hora', type: 'warning', delay: 25000 },
            { msg: 'Você subiu 15 LP!', type: 'success', delay: 35000 }
        ];
        
        notifications.forEach(notif => {
            setTimeout(() => {
                NotificationSystem.show(notif.msg, notif.type);
            }, notif.delay);
        });
    }
}

// ==================== ANIMAÇÃO DE NÚMEROS ====================
class CountUpAnimation {
    static animateNumber(element, target, duration = 1000) {
        const start = parseInt(element.textContent) || 0;
        const startTime = Date.now();
        
        const animate = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const current = Math.floor(start + (target - start) * progress);
            
            element.textContent = current;
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };
        
        animate();
    }
}

// ==================== DASHBOARD INTERATIVO ====================
class DashboardInteractive {
    static updateStats() {
        const statsElements = {
            matches: document.querySelector('[data-stat="matches"]'),
            winRate: document.querySelector('[data-stat="winRate"]'),
            victories: document.querySelector('[data-stat="victories"]')
        };
        
        if (statsElements.matches) {
            CountUpAnimation.animateNumber(statsElements.matches, SITE_DATA.user.stats.matches);
        }
        if (statsElements.winRate) {
            CountUpAnimation.animateNumber(statsElements.winRate, SITE_DATA.user.stats.winRate);
        }
        if (statsElements.victories) {
            CountUpAnimation.animateNumber(statsElements.victories, SITE_DATA.user.stats.victories);
        }
    }
    
    static addQuickActionListeners() {
        document.querySelectorAll('.quick-action-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const action = btn.textContent.toLowerCase();
                if (action.includes('torneio')) {
                    NotificationSystem.show('Modal de criar torneio aberto', 'info');
                } else if (action.includes('jogador')) {
                    NotificationSystem.show('Convite enviado aos jogadores!', 'success');
                } else if (action.includes('config')) {
                    NotificationSystem.show('Abrindo configurações...', 'info');
                }
            });
        });
    }
}

// ==================== CARROSSEL DE VÍDEOS ====================
class VideoCarousel {
    static init() {
        // Adicionar navegação entre vídeos
        document.querySelectorAll('.video-card').forEach((card, idx) => {
            card.addEventListener('click', () => {
                NotificationSystem.show(`Vídeo ${idx + 1} selecionado`, 'info');
            });
        });
    }
}

// ==================== EFEITOS DE PÁGINA ====================
class PageEffects {
    static addPageTransition() {
        document.querySelectorAll('a[href*=".html"]').forEach(link => {
            link.addEventListener('click', (e) => {
                if (!link.target) {
                    e.preventDefault();
                    const href = link.getAttribute('href');
                    
                    document.body.style.opacity = '0.5';
                    setTimeout(() => {
                        window.location.href = href;
                    }, 300);
                }
            });
        });
    }
    
    static loadingBar() {
        window.addEventListener('beforeunload', () => {
            const bar = document.createElement('div');
            bar.className = 'loading-bar';
            document.body.appendChild(bar);
            bar.style.width = '80%';
        });
    }
}

// ==================== SISTEMA DE TEMA ====================
class ThemeSystem {
    static toggleTheme() {
        const isDark = document.body.classList.toggle('dark-theme');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
        NotificationSystem.show(isDark ? '🌙 Tema Escuro' : '☀️ Tema Claro', 'info');
    }
    
    static loadTheme() {
        const theme = localStorage.getItem('theme') || 'dark';
        if (theme === 'dark') {
            document.body.classList.add('dark-theme');
        }
    }
}

// ==================== INICIALIZAÇÃO ====================
document.addEventListener('DOMContentLoaded', () => {
    console.log('🎮 Kitsune Academy - Sistema Interativo Carregado');
    
    // Inicializar todos os sistemas
    ScrollAnimations.init();
    AdvancedHoverEffects.init();
    RealtimeCounters.init();
    VideoCarousel.init();
    PageEffects.addPageTransition();
    ThemeSystem.loadTheme();
    
    // Dashboard específico
    if (window.location.pathname.includes('dashboard')) {
        setTimeout(() => {
            DashboardInteractive.updateStats();
            DashboardInteractive.addQuickActionListeners();
        }, 100);
    }
    
    // Notificações em tempo real após 2s
    setTimeout(() => {
        LiveNotifications.init();
    }, 2000);
});

// ==================== FUNÇÕES GLOBAIS ====================
window.openRegisterModal = openRegisterModal;
window.joinTournament = joinTournament;
window.viewTeam = viewTeam;
window.toggleTheme = () => ThemeSystem.toggleTheme();
window.filterTournaments = (status) => FilterSystem.filterTournaments(status);
window.searchTeams = (query) => FilterSystem.searchTeams(query);
window.sortTeams = (sortBy) => FilterSystem.sortTeams(sortBy);

console.log('✅ Funções globais carregadas');
