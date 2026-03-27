import { useState } from "react";
import { useLocation } from "react-router-dom";
import logo from "/logo.png"
import "./style.css";

const navLinks = [
    { label: "Home", href: "/"},
    { label: "Times", href: "/teams" },
    { label: "Campeonatos", href: "/tournaments" },
    { label: "Ligas", href: "/leagues" },
    { label: "Ranking", href: "/ranking" },
];

export default function Index() {
    const [mobileOpen, setMobileOpen] = useState(false);
    const location = useLocation();

    return (
        <nav className="lol-navbar">
            <div className="nav-inner">

                {/* ESQUERDA — Logo */}
                <a href="/" className="nav-logo">
                    <img
                        src={logo}
                        alt="logo"
                        height="48"
                    />
                    <span className="logo-text">NKZ<span>Academy</span></span>
                </a>

                {/* CENTRO — Links */}
                <ul className="nav-links">
                    {navLinks.map((link) => (
                        <li key={link.label}>
                            <a href={link.href} className={location.pathname === link.href ? "active" : ""}>
                                {link.label}
                            </a>
                        </li>
                    ))}
                </ul>

                {/* DIREITA — Ações */}
                <div className="nav-actions">
                    <button className="btn-ghost">Entrar</button>
                    <button className="btn-primary-lol">Criar Conta</button>
                </div>

                {/* Mobile toggle */}
                <button
                    className="nav-toggle"
                    onClick={() => setMobileOpen(!mobileOpen)}
                >
                    {mobileOpen ? "✕" : "☰"}
                </button>
            </div>

            {/* Mobile drawer */}
            <div className={`nav-mobile ${mobileOpen ? "open" : ""}`}>
                {navLinks.map((link) => (
                    <a key={link.label} href={link.href}>{link.label}</a>
                ))}
                <div className="mobile-actions">
                    <button className="btn-ghost">Entrar</button>
                    <button className="btn-primary-lol">Criar Conta</button>
                </div>
            </div>
        </nav>
    );
}