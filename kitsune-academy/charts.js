/**
 * ============================================================
 * KITSUNE ACADEMY - GRÁFICOS INTERATIVOS
 * ============================================================
 * Sistema de gráficos de performance com Chart.js
 */

// Configuração de cores do site
const CHART_COLORS = {
    primary: '#9c27b0',
    secondary: '#5c6cff',
    tertiary: '#ff5722',
    success: '#4caf50',
    warning: '#ff9800',
    danger: '#f44336',
    info: '#2196f3',
    dark: '#0a0e27',
    light: '#c0c0e0'
};

// Configuração padrão para os gráficos
const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            labels: {
                color: CHART_COLORS.light,
                font: {
                    family: "'Orbitron', sans-serif",
                    size: 12,
                    weight: 'bold'
                }
            }
        }
    },
    scales: {
        x: {
            grid: {
                color: 'rgba(156, 39, 176, 0.1)',
                drawBorder: false
            },
            ticks: {
                color: CHART_COLORS.light,
                font: {
                    family: "'Roboto', sans-serif"
                }
            }
        },
        y: {
            grid: {
                color: 'rgba(156, 39, 176, 0.1)',
                drawBorder: false
            },
            ticks: {
                color: CHART_COLORS.light,
                font: {
                    family: "'Roboto', sans-serif"
                }
            }
        }
    }
};

// Dados dos Campeões com Ícones
const CHAMPIONS = {
    'Ahri': { icon: '🦊', color: '#ff5c93', wins: 24, losses: 8 },
    'Lux': { icon: '✨', color: '#ffd700', wins: 18, losses: 7 },
    'Syndra': { icon: '🌙', color: '#9370db', wins: 15, losses: 5 },
    'Vel\'Koz': { icon: '👁️', color: '#00ffff', wins: 12, losses: 4 },
    'Xerath': { icon: '⚡', color: '#ff8c00', wins: 10, losses: 3 }
};

// Dados simulados
const simulatedData = {
    lpProgression: {
        labels: ['Semana 1', 'Semana 2', 'Semana 3', 'Semana 4', 'Semana 5', 'Semana 6', 'Esta Semana'],
        values: [980, 1050, 1120, 1180, 1210, 1230, 1245]
    },
    winRate: {
        labels: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago'],
        values: [58, 61, 59, 62, 65, 63, 62, 64]
    },
    champions: {
        labels: ['🦊 Ahri', '✨ Lux', '🌙 Syndra', '👁️ Vel\'Koz', '⚡ Xerath'],
        wins: [24, 18, 15, 12, 10],
        losses: [8, 7, 5, 4, 3]
    },
    matchDuration: {
        labels: ['20-25m', '25-30m', '30-35m', '35-40m', '40+m'],
        values: [8, 22, 35, 28, 15]
    }
};

/**
 * Gráfico de Progressão de LP
 */
function initLPChart() {
    const ctx = document.getElementById('lpChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: simulatedData.lpProgression.labels,
            datasets: [{
                label: 'Pontos (LP)',
                data: simulatedData.lpProgression.values,
                borderColor: CHART_COLORS.secondary,
                backgroundColor: 'rgba(92, 108, 255, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointBackgroundColor: CHART_COLORS.primary,
                pointBorderColor: CHART_COLORS.secondary,
                pointBorderWidth: 2,
                pointRadius: 5,
                pointHoverRadius: 8,
                shadowColor: 'rgba(92, 108, 255, 0.5)',
                shadowBlur: 10
            }]
        },
        options: {
            ...chartOptions,
            plugins: {
                ...chartOptions.plugins,
                tooltip: {
                    backgroundColor: 'rgba(156, 39, 176, 0.9)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: CHART_COLORS.secondary,
                    borderWidth: 2,
                    padding: 12,
                    displayColors: false,
                    callbacks: {
                        label: function(context) {
                            return 'LP: ' + context.parsed.y;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Gráfico de Win Rate Mensal
 */
function initWinRateChart() {
    const ctx = document.getElementById('winRateChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: simulatedData.winRate.labels,
            datasets: [{
                label: 'Win Rate (%)',
                data: simulatedData.winRate.values,
                backgroundColor: [
                    'rgba(76, 175, 80, 0.8)',
                    'rgba(76, 175, 80, 0.8)',
                    'rgba(255, 152, 0, 0.8)',
                    'rgba(76, 175, 80, 0.8)',
                    'rgba(156, 39, 176, 0.8)',
                    'rgba(92, 108, 255, 0.8)',
                    'rgba(76, 175, 80, 0.8)',
                    'rgba(76, 175, 80, 0.8)'
                ],
                borderColor: CHART_COLORS.secondary,
                borderWidth: 2,
                borderRadius: 8,
                borderSkipped: false,
                hoverBackgroundColor: CHART_COLORS.primary
            }]
        },
        options: {
            ...chartOptions,
            indexAxis: undefined,
            plugins: {
                ...chartOptions.plugins,
                tooltip: {
                    backgroundColor: 'rgba(156, 39, 176, 0.9)',
                    callbacks: {
                        label: function(context) {
                            return 'Win Rate: ' + context.parsed.y + '%';
                        }
                    }
                }
            }
        }
    });
}

/**
 * Gráfico de Desempenho por Campeão
 */
function initChampionChart() {
    const ctx = document.getElementById('championChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: simulatedData.champions.labels,
            datasets: [
                {
                    label: 'Vitórias',
                    data: simulatedData.champions.wins,
                    borderColor: CHART_COLORS.success,
                    backgroundColor: 'rgba(76, 175, 80, 0.2)',
                    borderWidth: 2,
                    pointBackgroundColor: CHART_COLORS.success,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                },
                {
                    label: 'Derrotas',
                    data: simulatedData.champions.losses,
                    borderColor: CHART_COLORS.danger,
                    backgroundColor: 'rgba(244, 67, 54, 0.2)',
                    borderWidth: 2,
                    pointBackgroundColor: CHART_COLORS.danger,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                }
            ]
        },
        options: {
            ...chartOptions,
            plugins: {
                ...chartOptions.plugins,
                tooltip: {
                    backgroundColor: 'rgba(156, 39, 176, 0.9)'
                }
            },
            scales: {
                r: {
                    grid: {
                        color: 'rgba(156, 39, 176, 0.2)',
                        drawBorder: false
                    },
                    ticks: {
                        color: CHART_COLORS.light,
                        font: {
                            family: "'Roboto', sans-serif"
                        }
                    }
                }
            }
        }
    });
}

/**
 * Gráfico de Duração das Partidas
 */
function initMatchDurationChart() {
    const ctx = document.getElementById('matchDurationChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: simulatedData.matchDuration.labels,
            datasets: [{
                label: 'Partidas',
                data: simulatedData.matchDuration.values,
                backgroundColor: [
                    CHART_COLORS.secondary,
                    CHART_COLORS.primary,
                    CHART_COLORS.tertiary,
                    CHART_COLORS.warning,
                    CHART_COLORS.info
                ],
                borderColor: '#0a0e27',
                borderWidth: 2,
                hoverBorderColor: CHART_COLORS.secondary,
                hoverBorderWidth: 3
            }]
        },
        options: {
            ...chartOptions,
            plugins: {
                ...chartOptions.plugins,
                tooltip: {
                    backgroundColor: 'rgba(156, 39, 176, 0.9)',
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return label + ': ' + value + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });
}

/**
 * Inicializar todos os gráficos
 */
function initializeCharts() {
    console.log('📊 Inicializando gráficos...');
    
    // Aguardar um pouco para garantir que o DOM está pronto
    setTimeout(() => {
        initLPChart();
        initWinRateChart();
        initChampionChart();
        initMatchDurationChart();
        console.log('✅ Todos os gráficos foram inicializados com sucesso!');
    }, 500);
}

// Inicializar quando o DOM estiver pronto
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeCharts);
} else {
    initializeCharts();
}

/**
 * Função para animar gráficos (com atualização de dados)
 */
function animateChartData(chartInstance, newData, animationDuration = 1000) {
    if (!chartInstance) return;
    
    chartInstance.data.datasets[0].data = newData;
    chartInstance.update('active');
}

/**
 * Simular atualização de dados em tempo real
 */
function simulateRealtimeUpdate() {
    const lpChartCanvas = document.getElementById('lpChart');
    if (!lpChartCanvas) return;
    
    // A cada 10 segundos, simular novo LP
    setInterval(() => {
        const currentLP = simulatedData.lpProgression.values[simulatedData.lpProgression.values.length - 1];
        const newLP = currentLP + Math.floor(Math.random() * 20) - 5; // -5 a +15
        
        console.log(`🎮 LP Atualizado: ${newLP}`);
    }, 10000);
}

// Iniciar simulação de atualização em tempo real
window.addEventListener('load', () => {
    setTimeout(simulateRealtimeUpdate, 2000);
});

// Exportar funções para uso global
window.initializeCharts = initializeCharts;
window.animateChartData = animateChartData;
