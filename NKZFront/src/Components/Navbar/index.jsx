import { useEffect, useState } from "react";
import axios from "axios";
import { useLocation, useNavigate } from "react-router-dom";
import { FaChevronDown, FaRegBell, FaUserShield } from "react-icons/fa";
import { clearSession, getAuthHeaders, getCurrentUser } from "../../utils/auth";
import logo from "/logo.png"
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

const navLinks = [
    { label: "Home", href: "/"},
    { label: "Dashboard", href: "/dashboard"},
    { label: "Times", href: "/teams" },
    { label: "Jogadores", href: "/players" },
    { label: "Campeonatos", href: "/tournaments" },
    { label: "Ligas", href: "/leagues" },
    { label: "Ranking", href: "/ranking" },
];

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? null;
}

function resolveImageUrl(url) {
    if (!url) return "";
    if (/^https?:\/\//i.test(url)) return url;
    return `${API_BASE_URL}/${url}`.replace(/([^:]\/)\/+/g, "$1");
}

export default function Index() {
    const [mobileOpen, setMobileOpen] = useState(false);
    const [userMenuOpen, setUserMenuOpen] = useState(false);
    const [profileImageUrl, setProfileImageUrl] = useState("");
    const [profileDisplayName, setProfileDisplayName] = useState("");
    const location = useLocation();
    const navigate = useNavigate();
    const currentUser = getCurrentUser();
    const userLabel = profileDisplayName || currentUser?.email || "Usuario";

    useEffect(() => {
        let isMounted = true;

        async function loadProfileImage() {
            if (!currentUser?.userId) {
                setProfileImageUrl("");
                setProfileDisplayName("");
                return;
            }

            try {
                const response = await axios.get(`${API_BASE_URL}/api/player/user/${currentUser.userId}`, {
                    headers: getAuthHeaders(),
                });
                const player = unwrapApiData(response.data);
                const imageUrl = player?.profileImageUrl ?? player?.ProfileImageUrl ?? "";
                const summonerName = player?.summonerName ?? player?.SummonerName ?? "";
                if (isMounted) {
                    setProfileImageUrl(resolveImageUrl(imageUrl));
                    setProfileDisplayName(summonerName);
                }
            } catch {
                if (isMounted) {
                    setProfileImageUrl("");
                    setProfileDisplayName("");
                }
            }
        }

        loadProfileImage();
        window.addEventListener("nkz-profile-image-updated", loadProfileImage);
        window.addEventListener("nkz-player-synced", loadProfileImage);

        return () => {
            isMounted = false;
            window.removeEventListener("nkz-profile-image-updated", loadProfileImage);
            window.removeEventListener("nkz-player-synced", loadProfileImage);
        };
    }, [currentUser?.userId]);

    function handleLogout() {
        clearSession();
        setMobileOpen(false);
        setUserMenuOpen(false);
        navigate("/");
    }

    function navigateAndClose(path) {
        setMobileOpen(false);
        setUserMenuOpen(false);
        navigate(path);
    }

    function renderUserMenu(mobile = false) {
        return (
            <div className={mobile ? "mobile-user-menu" : "user-menu"}>
                <button
                    className="user-menu-button"
                    onClick={() => setUserMenuOpen((open) => !open)}
                    aria-expanded={userMenuOpen}
                    aria-label="Menu do usuario"
                >
                    <span className="user-menu-avatar">
                        <FaUserShield className="user-menu-avatar-fallback" />
                        {profileImageUrl && (
                            <img
                                src={profileImageUrl}
                                alt={userLabel}
                                onError={(event) => {
                                    event.currentTarget.style.display = "none";
                                }}
                            />
                        )}
                    </span>
                    <span className="user-menu-name">{userLabel}</span>
                    <FaChevronDown className={`user-menu-arrow ${userMenuOpen ? "open" : ""}`} />
                </button>

                <div className={`user-menu-dropdown ${userMenuOpen ? "open" : ""}`}>
                    <button onClick={() => navigateAndClose("/notifications")}>
                        <FaRegBell /> Notificacoes
                    </button>
                    <button onClick={() => navigateAndClose("/dashboard")}>
                        <FaUserShield /> Dashboard
                    </button>
                    <button onClick={handleLogout}>Deslogar</button>
                </div>
            </div>
        );
    }

    return (
        <nav className="lol-navbar">
            <div className="nav-inner">
                <a href="/" className="nav-logo">
                    <img
                        src={logo}
                        alt="logo"
                        height="48"
                    />
                    <span className="logo-text">NKZ<span>Academy</span></span>
                </a>

                <ul className="nav-links">
                    {navLinks.map((link) => (
                        <li key={link.label}>
                            <a href={link.href} className={location.pathname === link.href ? "active" : ""}>
                                {link.label}
                            </a>
                        </li>
                    ))}
                </ul>

                <div className="nav-actions">
                    {currentUser ? (
                        renderUserMenu()
                    ) : (
                        <>
                            <button className="btn-ghost" onClick={() => navigateAndClose("/login")}>Entrar</button>
                            <button className="btn-primary-lol" onClick={() => navigateAndClose("/register")}>Criar Conta</button>
                        </>
                    )}
                </div>

                <button
                    className="nav-toggle"
                    onClick={() => setMobileOpen(!mobileOpen)}
                    aria-label="Abrir menu"
                >
                    {mobileOpen ? "x" : "☰"}
                </button>
            </div>

            <div className={`nav-mobile ${mobileOpen ? "open" : ""}`}>
                {navLinks.map((link) => (
                    <a key={link.label} href={link.href}>{link.label}</a>
                ))}
                <div className="mobile-actions">
                    {currentUser ? (
                        renderUserMenu(true)
                    ) : (
                        <>
                            <button className="btn-ghost" onClick={() => navigateAndClose("/login")}>Entrar</button>
                            <button className="btn-primary-lol" onClick={() => navigateAndClose("/register")}>Criar Conta</button>
                        </>
                    )}
                </div>
            </div>
        </nav>
    );
}
