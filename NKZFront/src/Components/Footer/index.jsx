import logo from "/logo.png";
import { FaDiscord, FaYoutube, FaInstagram } from "react-icons/fa";
import { FaXTwitter } from "react-icons/fa6";
import "./style.css";

const footerLinks = {
    plataforma: [
        { label: "Home", href: "/" },
        { label: "Times", href: "/teams" },
        { label: "Campeonatos", href: "/tournaments" },
        { label: "Ligas", href: "/leagues" },
        { label: "Ranking", href: "/ranking" },
    ],
    conta: [
        { label: "Criar Conta", href: "/register" },
        { label: "Entrar", href: "/login" },
        { label: "Vincular Riot ID", href: "/profile/riot" },
        { label: "Meu Perfil", href: "/profile" },
    ],
    suporte: [
        { label: "Sobre o Projeto", href: "/about" },
        { label: "Regras", href: "/rules" },
        { label: "Contato", href: "/contact" },
        { label: "Termos de Uso", href: "/terms" },
    ],
};


export default function Footer() {
    const year = new Date().getFullYear();

    const socials = [
        { label: "Discord", icon: <FaDiscord/>, href: "#" },
        { label: "Twitter", icon: <FaXTwitter/>, href: "#" },
        { label: "YouTube", icon: <FaYoutube/>, href: "#" },
        { label: "Instagram", icon: <FaInstagram/>, href: "#" },
    ];


    return (
        <footer className="lol-footer fixed-bottom">

            {/* ─── Seção principal ─── */}
            <div className="footer-main">

                {/* Marca */}
                <div className="footer-brand">
                    <a href="/" className="footer-logo">
                        <img src={logo} alt="NKZ Academy" height="40" />
                        <span className="footer-logo-text">NKZ<span>Academy</span></span>
                    </a>
                    <p>
                        A plataforma competitiva para jogadores de League of Legends.
                        Crie seu time, dispute campeonatos e suba no ranking.
                    </p>
                    <div className="footer-socials">
                        {socials.map((s) => (
                            <a key={s.label} href={s.href} className="social-btn" title={s.label}>
                                {s.icon}
                            </a>
                        ))}
                    </div>
                </div>

                {/* Plataforma */}
                <div className="footer-col">
                    <h4>Plataforma</h4>
                    <ul>
                        {footerLinks.plataforma.map((l) => (
                            <li key={l.label}><a href={l.href}>{l.label}</a></li>
                        ))}
                    </ul>
                </div>

                {/* Conta */}
                <div className="footer-col">
                    <h4>Conta</h4>
                    <ul>
                        {footerLinks.conta.map((l) => (
                            <li key={l.label}><a href={l.href}>{l.label}</a></li>
                        ))}
                    </ul>
                </div>

                {/* Suporte */}
                <div className="footer-col">
                    <h4>Suporte</h4>
                    <ul>
                        {footerLinks.suporte.map((l) => (
                            <li key={l.label}><a href={l.href}>{l.label}</a></li>
                        ))}
                    </ul>
                </div>

            </div>

            <div className="footer-divider" />

            {/* ─── Rodapé inferior ─── */}
            <div className="footer-bottom">
                <p>
                    © {year} NKZ Academy. Feito por{" "}
                    <a href="#">Clã NKZ</a>.
                    Não afiliado à Riot Games.
                </p>
                <div className="footer-badge">
                    Feito com <span>♥</span> para a comunidade LoL
                </div>
            </div>

        </footer>
    );
}