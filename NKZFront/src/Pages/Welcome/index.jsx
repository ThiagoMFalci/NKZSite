import { useNavigate } from "react-router-dom";
import { BsTrophyFill, BsPeopleFill, BsShieldFillCheck } from "react-icons/bs";
import { GiCrossedSwords, GiRank3, GiTrophy } from "react-icons/gi";
import "./style.css";

const features = [
    {
        color: "purple",
        icon: <GiCrossedSwords />,
        title: "Campeonatos",
        desc: "Crie ou participe de campeonatos organizados. Defina formato, datas e regras do seu jeito.",
    },
    {
        color: "blue",
        icon: <BsPeopleFill />,
        title: "Times",
        desc: "Monte seu time, gerencie membros e aceite candidaturas de jogadores da comunidade.",
    },
    {
        color: "orange",
        icon: <BsTrophyFill />,
        title: "Ligas",
        desc: "Dispute ligas contínuas e acumule pontos ao longo da temporada para subir no ranking.",
    },
    {
        color: "purple",
        icon: <BsShieldFillCheck />,
        title: "Verificação Riot",
        desc: "Vincule sua conta da Riot para validar seu perfil e exibir seu rank atual.",
    },
    {
        color: "blue",
        icon: <GiRank3 />,
        title: "Ranking",
        desc: "Acompanhe sua posição no ranking geral baseado em vitórias e desempenho nos campeonatos.",
    },
    {
        color: "orange",
        icon: <GiTrophy />,
        title: "Histórico",
        desc: "Seu histórico de partidas, campeonatos e conquistas disponível no seu perfil.",
    },
];

const steps = [
    { num: "01", title: "Crie sua conta", desc: "Registre-se gratuitamente e vincule seu Riot ID para validar seu perfil." },
    { num: "02", title: "Entre num time", desc: "Crie seu próprio time ou candidate-se a times que estão recrutando." },
    { num: "03", title: "Inscreva-se", desc: "Escolha um campeonato ou liga e inscreva seu time para competir." },
    { num: "04", title: "Compita", desc: "Jogue suas partidas, acumule pontos e suba no ranking da plataforma." },
];

export default function Index() {
    const navigate = useNavigate();

    return (
        <div className="home-page">

            {/* ─── HERO ─── */}
            <section className="hero">
                <div className="hero-bg-grid" />
                <div className="hero-glow" />
                <div className="hero-glow-2" />
                <div className="hero-hex">⬡</div>
                <div className="hero-hex">⬡</div>
                <div className="hero-hex">⬡</div>
                <div className="hero-hex">⬡</div>

                <div className="hero-content">
                    <div className="hero-badge">
                        <span className="hero-badge-dot" />
                        Plataforma competitiva de LoL
                    </div>

                    <h1 className="hero-title">
                        <span className="hero-title-line1">Sua arena</span>
                        <span className="hero-title-line2">competitiva</span>
                    </h1>

                    <p className="hero-sub">
                        Crie times, dispute campeonatos e ligas de League of Legends.
                        A plataforma feita pela comunidade NKZ para jogadores sérios.
                    </p>

                    <div className="hero-actions">
                        <button className="btn-primary" onClick={() => navigate("/register")}>
                            Criar conta grátis
                        </button>
                        <button className="btn-ghost" onClick={() => navigate("/tournaments")}>
                            Ver campeonatos
                        </button>
                    </div>

                    <div className="hero-stats">
                        <div className="stat-item">
                            <div className="stat-number">120<span>+</span></div>
                            <div className="stat-label">Jogadores</div>
                        </div>
                        <div className="stat-item">
                            <div className="stat-number">24<span>+</span></div>
                            <div className="stat-label">Campeonatos</div>
                        </div>
                        <div className="stat-item">
                            <div className="stat-number">18<span>+</span></div>
                            <div className="stat-label">Times ativos</div>
                        </div>
                        <div className="stat-item">
                            <div className="stat-number">6<span>+</span></div>
                            <div className="stat-label">Ligas</div>
                        </div>
                    </div>
                </div>
            </section>

            <div className="divider-line" />

            {/* ─── FEATURES ─── */}
            <div className="section">
                <p className="section-eyebrow">Recursos</p>
                <h2 className="section-title">Tudo que você <span>precisa</span></h2>
                <p className="section-desc">
                    Da criação de conta até o pódio do campeonato — tudo em um só lugar.
                </p>

                <div className="features-grid">
                    {features.map((f) => (
                        <div key={f.title} className={`feature-card ${f.color}`}>
                            <div className={`feature-icon ${f.color}`}>{f.icon}</div>
                            <div className="feature-title">{f.title}</div>
                            <p className="feature-desc">{f.desc}</p>
                        </div>
                    ))}
                </div>
            </div>

            {/* ─── HOW IT WORKS ─── */}
            <section className="how-section">
                <div className="how-inner">
                    <p className="section-eyebrow">Como funciona</p>
                    <h2 className="section-title">Comece a <span>competir</span></h2>
                    <p className="section-desc">
                        Em 4 passos simples você já está dentro de um campeonato.
                    </p>

                    <div className="steps-grid">
                        {steps.map((s) => (
                            <div key={s.num} className="step-item">
                                <div className="step-number">{s.num}</div>
                                <div className="step-title">{s.title}</div>
                                <p className="step-desc">{s.desc}</p>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* ─── CTA FINAL ─── */}
            <section className="cta-section">
                <div className="cta-inner">
                    <h2 className="cta-title">
                        Pronto para <span>competir?</span>
                    </h2>
                    <p className="cta-desc">
                        Junte-se à comunidade NKZ, monte seu time e mostre do que você é capaz nos campeonatos.
                    </p>
                    <div className="hero-actions">
                        <button className="btn-primary" onClick={() => navigate("/register")}>
                            Criar conta grátis
                        </button>
                        <button className="btn-ghost" onClick={() => navigate("/login")}>
                            Já tenho conta
                        </button>
                    </div>
                </div>
            </section>

        </div>
    );
}