import { useEffect, useState } from "react";
import axios from "axios";
import { useLocation, useNavigate } from "react-router-dom";
import { FaCalendarAlt, FaChevronDown, FaPlus, FaRegBell, FaUserShield, FaWallet } from "react-icons/fa";
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
    const [pendingNotifications, setPendingNotifications] = useState(0);
    const [pendingSchedules, setPendingSchedules] = useState(0);
    const [walletBalance, setWalletBalance] = useState(0);
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
                setPendingNotifications(0);
                setPendingSchedules(0);
                setWalletBalance(0);
                return;
            }

            try {
                const response = await axios.get(`${API_BASE_URL}/api/player/user/${currentUser.userId}`, {
                    headers: getAuthHeaders(),
                });
                const player = unwrapApiData(response.data);
                const imageUrl = player?.profileImageUrl ?? player?.ProfileImageUrl ?? "";
                const summonerName = player?.summonerName ?? player?.SummonerName ?? "";
                let pendingCount = 0;

                if (player?.id ?? player?.Id) {
                    const invitationsResponse = await axios.get(`${API_BASE_URL}/api/team/players/${player.id ?? player.Id}/invitations`, {
                        headers: getAuthHeaders(),
                    }).catch(() => ({ data: [] }));
                    pendingCount += (unwrapApiData(invitationsResponse.data) || [])
                        .filter((invite) => (invite.status ?? invite.Status) === "Pending").length;
                }

                const teamsResponse = await axios.get(`${API_BASE_URL}/api/team/ListTeams`, {
                    headers: getAuthHeaders(),
                }).catch(() => ({ data: [] }));
                const manageableTeams = (unwrapApiData(teamsResponse.data) || []).filter((team) => (
                    (team.ownerId ?? team.OwnerId) === currentUser.userId ||
                    (team.players ?? team.Players ?? []).some((teamPlayer) => (
                        (teamPlayer.userId ?? teamPlayer.UserId) === currentUser.userId &&
                        (teamPlayer.isCaptain ?? teamPlayer.IsCaptain)
                    ))
                ));

                const teamInvitationLists = await Promise.all(manageableTeams.map(async (team) => {
                    const teamId = team.id ?? team.Id;
                    const response = await axios.get(`${API_BASE_URL}/api/team/${teamId}/invitations`, {
                        headers: getAuthHeaders(),
                    }).catch(() => ({ data: [] }));
                    return unwrapApiData(response.data) || [];
                }));
                pendingCount += teamInvitationLists.flat()
                    .filter((invite) => (invite.status ?? invite.Status) === "Pending").length;

                const manageableTeamIds = new Set(manageableTeams.map((team) => team.id ?? team.Id));
                const leaguesResponse = await axios.get(`${API_BASE_URL}/api/league/ListLeagues`, {
                    headers: getAuthHeaders(),
                }).catch(() => ({ data: [] }));
                const scheduleCount = (unwrapApiData(leaguesResponse.data) || []).flatMap((league) => league.matches ?? league.Matches ?? [])
                    .filter((match) => {
                        const teamAId = match.teamAId ?? match.TeamAId;
                        const teamBId = match.teamBId ?? match.TeamBId;
                        const status = match.status ?? match.Status;
                        const scheduleStatus = match.scheduleStatus ?? match.ScheduleStatus ?? "Open";
                        return status !== "Completed" &&
                            (manageableTeamIds.has(teamAId) || manageableTeamIds.has(teamBId)) &&
                            ["Open", "Pending", "Rejected"].includes(scheduleStatus);
                    }).length;

                const walletResponse = await axios.get(`${API_BASE_URL}/api/auth/User/wallet`, {
                    headers: getAuthHeaders(),
                }).catch(() => ({ data: null }));
                const wallet = unwrapApiData(walletResponse.data);

                if (isMounted) {
                    setProfileImageUrl(resolveImageUrl(imageUrl));
                    setProfileDisplayName(summonerName);
                    setPendingNotifications(pendingCount);
                    setPendingSchedules(scheduleCount);
                    setWalletBalance(wallet?.balance ?? wallet?.Balance ?? 0);
                }
            } catch {
                if (isMounted) {
                    setProfileImageUrl("");
                    setProfileDisplayName("");
                    setPendingNotifications(0);
                    setPendingSchedules(0);
                    setWalletBalance(0);
                }
            }
        }

        loadProfileImage();
        window.addEventListener("nkz-profile-image-updated", loadProfileImage);
        window.addEventListener("nkz-player-synced", loadProfileImage);
        window.addEventListener("nkz-wallet-updated", loadProfileImage);

        return () => {
            isMounted = false;
            window.removeEventListener("nkz-profile-image-updated", loadProfileImage);
            window.removeEventListener("nkz-player-synced", loadProfileImage);
            window.removeEventListener("nkz-wallet-updated", loadProfileImage);
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
                <button className="wallet-chip" type="button" onClick={() => navigateAndClose("/wallet")} aria-label="Adicionar saldo">
                    <FaWallet />
                    <span>{Number(walletBalance || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</span>
                    <FaPlus />
                </button>
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
                    {pendingNotifications > 0 && (
                        <span className="notification-pulse" aria-label={`${pendingNotifications} notificacoes pendentes`}>
                            {pendingNotifications > 9 ? "9+" : pendingNotifications}
                        </span>
                    )}
                    <FaChevronDown className={`user-menu-arrow ${userMenuOpen ? "open" : ""}`} />
                </button>

                <div className={`user-menu-dropdown ${userMenuOpen ? "open" : ""}`}>
                    <button onClick={() => navigateAndClose("/notifications")}>
                        <FaRegBell /> Notificacoes
                    </button>
                    <button onClick={() => navigateAndClose("/notifications?tab=schedules")}>
                        <FaCalendarAlt /> Agendamentos
                        {pendingSchedules > 0 && <span className="menu-count">{pendingSchedules > 9 ? "9+" : pendingSchedules}</span>}
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
