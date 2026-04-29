import { useEffect, useState } from "react";
import axios from "axios";
import ChampionStats from "./components/ChampionStats";
import PlayerHistory from "./components/PlayerHistory";
import RankCard from "./components/RankCard";
import RoleStats from "./components/RoleStats";
import StatsOverview from "./components/StatsOverview";
import UserHeader from "./components/UserHeader";
import { getCurrentUser, getAuthHeaders } from "../../utils/auth";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const PLAYER_TAGS = ["Comunicativo", "Pool Vasta", "Flex", "Macro bom", "Micro bom", "Treino intenso"];
const ROLE_OPTIONS = ["Top", "Jungle", "Mid", "ADC", "Support", "Flex"];

function getStoredUserId() {
    return (
        localStorage.getItem("userId") ||
        localStorage.getItem("UserId") ||
        localStorage.getItem("id") ||
        localStorage.getItem("Id")
    );
}

function unwrapApiData(responseData) {
    if (Array.isArray(responseData)) return responseData;
    return responseData?.data ?? responseData?.Data ?? responseData;
}

function calculateWinRate(wins = 0, losses = 0, matches = 0) {
    const total = matches || wins + losses;
    return total > 0 ? Math.round((wins / total) * 100) : 0;
}

function normalizeChampionStats(champions = []) {
    return champions
        .map((champion) => {
            const wins = champion.wins ?? champion.Wins ?? 0;
            const losses = champion.losses ?? champion.Losses ?? 0;
            const matches = champion.matches ?? champion.totalMatches ?? champion.TotalMatches ?? wins + losses;

            return {
                name: champion.name ?? champion.championName ?? champion.ChampionName ?? "Campeao",
                imageUrl: champion.imageUrl ?? champion.iconUrl ?? champion.ImageUrl ?? "",
                matches,
                wins,
                losses,
                winRate: champion.winRate ?? champion.WinRate ?? calculateWinRate(wins, losses, matches),
            };
        })
        .sort((a, b) => b.matches - a.matches);
}

function normalizeRoleStats(roles = {}) {
    const roleEntries = Array.isArray(roles)
        ? roles.reduce((acc, role) => {
            const key = (role.role ?? role.lane ?? role.position ?? "").toUpperCase();
            acc[key] = role;
            return acc;
        }, {})
        : roles;

    return ["TOP", "JUNGLE", "MID", "ADC", "SUPPORT"].reduce((acc, roleKey) => {
        const role = roleEntries?.[roleKey] ?? roleEntries?.[roleKey.toLowerCase()] ?? {};
        const wins = role.wins ?? role.Wins ?? 0;
        const losses = role.losses ?? role.Losses ?? 0;
        const matches = role.matches ?? role.totalMatches ?? role.TotalMatches ?? wins + losses;

        acc[roleKey] = {
            matches,
            wins,
            losses,
            winRate: role.winRate ?? role.WinRate ?? calculateWinRate(wins, losses, matches),
        };

        return acc;
    }, {});
}

function normalizeRecentMatches(matches = []) {
    return (Array.isArray(matches) ? matches : []).slice(0, 5).map((match, index) => {
        const champion = match.championName ?? match.ChampionName ?? match.champion ?? match.Champion ?? "Campeao";
        const result = match.result ?? match.Result ?? (match.win ?? match.Win ? "Vitoria" : "Derrota");
        return {
            id: match.id ?? match.Id ?? `${champion}-${index}`,
            champion,
            result,
            mode: match.mode ?? match.Mode ?? match.queueType ?? match.QueueType ?? "Ranqueada",
            kda: match.kda ?? match.Kda ?? `${match.kills ?? match.Kills ?? 0}/${match.deaths ?? match.Deaths ?? 0}/${match.assists ?? match.Assists ?? 0}`,
        };
    });
}

function normalizeEloHistory(history = [], currentRank = {}) {
    const data = Array.isArray(history) ? history : [];
    if (!data.length) {
        return [{
            label: "Atual",
            tier: currentRank.tier,
            division: currentRank.division,
            lp: currentRank.leaguePoints,
        }];
    }

    return data.slice(-6).map((entry, index) => ({
        label: entry.label ?? entry.Label ?? entry.date ?? entry.Date ?? `P${index + 1}`,
        tier: entry.tier ?? entry.Tier ?? "UNRANKED",
        division: entry.division ?? entry.Division ?? "",
        lp: entry.lp ?? entry.LP ?? entry.leaguePoints ?? entry.LeaguePoints ?? 0,
    }));
}

function normalizeTeamForCaptain(team, userId) {
    const players = team.players ?? team.Players ?? [];
    const ownerId = team.ownerId ?? team.OwnerId;
    const isCaptain = players.some((player) => (player.userId ?? player.UserId) === userId && (player.isCaptain ?? player.IsCaptain));
    if (ownerId !== userId && !isCaptain) return null;

    const roles = ["Top", "Jungle", "Mid", "ADC", "Support"];
    const filled = players.map((player) => String(player.mainRole ?? player.MainRole ?? player.role ?? player.Role ?? "Flex").toLowerCase());

    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        tag: team.tag ?? team.Tag ?? "TIME",
        players: players.length,
        missingRoles: roles.filter((role) => !filled.includes(role.toLowerCase())),
    };
}

function normalizeDashboardData(rawData) {
    const player = Array.isArray(rawData) ? rawData[0] : rawData;
    const wins = player?.wins ?? player?.Wins ?? 0;
    const losses = player?.losses ?? player?.Losses ?? 0;
    const totalMatches = player?.totalMatches ?? player?.TotalMatches ?? wins + losses;

    const profileImageUrl = player?.profileImageUrl ?? player?.ProfileImageUrl ?? player?.profileIconUrl ?? player?.ProfileIconUrl ?? "";

    const rank = {
        tier: player?.soloQueueTier ?? player?.SoloQueueTier ?? "UNRANKED",
        division: player?.soloQueueRank ?? player?.SoloQueueRank ?? "",
        leaguePoints: player?.soloQueueLP ?? player?.SoloQueueLP ?? 0,
        emblemUrl: player?.rankEmblemUrl ?? player?.RankEmblemUrl ?? "",
    };

    return {
        user: {
            id: player?.id ?? player?.Id,
            summonerName: player?.summonerName ?? player?.SummonerName ?? "Invocador",
            summonerLevel: player?.summonerLevel ?? player?.SummonerLevel ?? 0,
            profileIconUrl: profileImageUrl && !/^https?:\/\//i.test(profileImageUrl)
                ? `${API_BASE_URL}/${profileImageUrl}`.replace(/([^:]\/)\/+/g, "$1")
                : profileImageUrl,
        },
        rank,
        stats: {
            totalMatches,
            wins,
            losses,
            winRate: calculateWinRate(wins, losses, totalMatches),
        },
        champions: normalizeChampionStats(player?.championStats ?? player?.ChampionStats ?? []),
        roles: normalizeRoleStats(player?.roleStats ?? player?.RoleStats ?? {}),
        competitive: {
            mainRole: player?.mainRole ?? player?.MainRole ?? "Flex",
            lookingForTeam: player?.lookingForTeam ?? player?.LookingForTeam ?? true,
            tags: String(player?.tags ?? player?.Tags ?? "")
                .split(",")
                .map((tag) => tag.trim())
                .filter(Boolean),
        },
        history: {
            recentMatches: normalizeRecentMatches(player?.recentMatches ?? player?.RecentMatches ?? []),
            eloHistory: normalizeEloHistory(player?.eloHistory ?? player?.EloHistory ?? [], rank),
        },
    };
}

export default function DashboardPage() {
    const [dashboardData, setDashboardData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [summonerName, setSummonerName] = useState("");
    const [syncLoading, setSyncLoading] = useState(false);
    const [syncFeedback, setSyncFeedback] = useState({ type: "", message: "" });
    const [profileLoading, setProfileLoading] = useState(false);
    const [profileFeedback, setProfileFeedback] = useState({ type: "", message: "" });
    const [competitiveLoading, setCompetitiveLoading] = useState(false);
    const [competitiveFeedback, setCompetitiveFeedback] = useState({ type: "", message: "" });
    const [captainTeams, setCaptainTeams] = useState([]);
    const [captainRequests, setCaptainRequests] = useState([]);
    const [profileImageFile, setProfileImageFile] = useState(null);
    const [profileImagePreview, setProfileImagePreview] = useState("");
    const currentUser = getCurrentUser();

    async function loadDashboardData(isMounted = () => true) {
        try {
            setLoading(true);
            setError("");

            const userId = getStoredUserId();
            const endpoint = userId ? `/api/player/user/${userId}` : "/api/player";
            const response = await axios.get(`${API_BASE_URL}${endpoint}`, {
                headers: getAuthHeaders(),
            });

            if (!isMounted()) return;

            const data = unwrapApiData(response.data);
            setDashboardData(normalizeDashboardData(data));
        } catch (requestError) {
            if (!isMounted()) return;
            setError(requestError?.response?.data?.message || "Erro ao carregar dados");
        } finally {
            if (isMounted()) setLoading(false);
        }
    }

    useEffect(() => {
        let isMounted = true;

        loadDashboardData(() => isMounted);

        return () => {
            isMounted = false;
        };
    }, []);

    useEffect(() => {
        return () => {
            if (profileImagePreview) URL.revokeObjectURL(profileImagePreview);
        };
    }, [profileImagePreview]);

    useEffect(() => {
        let isMounted = true;

        async function loadCaptainPanel() {
            if (!currentUser?.userId) return;
            try {
                const teamsResponse = await axios.get(`${API_BASE_URL}/api/team/ListTeams`, { headers: getAuthHeaders() });
                const teams = (unwrapApiData(teamsResponse.data) || [])
                    .map((team) => normalizeTeamForCaptain(team, currentUser.userId))
                    .filter(Boolean);

                const requestLists = await Promise.all(teams.map(async (team) => {
                    try {
                        const response = await axios.get(`${API_BASE_URL}/api/team/${team.id}/invitations`, { headers: getAuthHeaders() });
                        return (unwrapApiData(response.data) || [])
                            .filter((invite) => String(invite.status ?? invite.Status).toLowerCase() === "pending")
                            .map((invite) => ({ id: invite.id ?? invite.Id, teamName: team.name, type: invite.type ?? invite.Type }));
                    } catch {
                        return [];
                    }
                }));

                if (isMounted) {
                    setCaptainTeams(teams);
                    setCaptainRequests(requestLists.flat());
                }
            } catch {
                if (isMounted) {
                    setCaptainTeams([]);
                    setCaptainRequests([]);
                }
            }
        }

        loadCaptainPanel();

        return () => {
            isMounted = false;
        };
    }, [currentUser?.userId]);

    async function handleSyncRiotPlayer(event) {
        event.preventDefault();
        const userId = currentUser?.userId || getStoredUserId();

        if (!userId) {
            setSyncFeedback({ type: "error", message: "Entre na sua conta para vincular um jogador." });
            return;
        }

        const riotId = summonerName.trim();
        if (!riotId) {
            setSyncFeedback({ type: "error", message: "Informe seu Riot ID." });
            return;
        }

        if (!riotId.includes("#")) {
            setSyncFeedback({ type: "error", message: "Use o formato Nome#TAG, por exemplo Thiago#BR1." });
            return;
        }

        try {
            setSyncLoading(true);
            setSyncFeedback({ type: "", message: "" });

            const response = await axios.put(
                `${API_BASE_URL}/api/player/${userId}/sync/${encodeURIComponent(riotId)}`,
                null,
                { headers: getAuthHeaders() }
            );

            const success = response.data?.success ?? response.data?.Success ?? true;
            if (!success) {
                setSyncFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Nao foi possivel vincular o jogador.",
                });
                return;
            }

            setSyncFeedback({ type: "success", message: "Riot ID vinculado com sucesso." });
            setSummonerName("");
            await loadDashboardData();
        } catch (requestError) {
            setSyncFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel vincular o jogador.",
            });
        } finally {
            setSyncLoading(false);
        }
    }

    function handleProfileImageSelect(file) {
        if (profileImagePreview) URL.revokeObjectURL(profileImagePreview);
        setProfileImageFile(file);
        setProfileImagePreview(file ? URL.createObjectURL(file) : "");
        if (file) setProfileFeedback({ type: "", message: "" });
    }

    async function handleProfileImageSave() {
        const userId = currentUser?.userId || getStoredUserId();

        if (!profileImageFile || !userId) return;

        try {
            setProfileLoading(true);
            setProfileFeedback({ type: "", message: "" });

            const formData = new FormData();
            formData.append("image", profileImageFile);

            const response = await axios.post(`${API_BASE_URL}/api/player/${userId}/profile-image`, formData, {
                headers: getAuthHeaders(),
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            if (!success) {
                setProfileFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Nao foi possivel trocar a imagem.",
                });
                return;
            }

            setProfileFeedback({ type: "success", message: "Imagem de perfil atualizada." });
            if (profileImagePreview) URL.revokeObjectURL(profileImagePreview);
            setProfileImageFile(null);
            setProfileImagePreview("");
            await loadDashboardData();
            window.dispatchEvent(new Event("nkz-profile-image-updated"));
        } catch (requestError) {
            setProfileFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel trocar a imagem.",
            });
        } finally {
            setProfileLoading(false);
        }
    }

    async function handleCompetitiveProfileSave(profile) {
        const userId = currentUser?.userId || getStoredUserId();
        if (!userId) return;

        try {
            setCompetitiveLoading(true);
            setCompetitiveFeedback({ type: "", message: "" });
            const response = await axios.put(`${API_BASE_URL}/api/player/${userId}/competitive-profile`, profile, {
                headers: getAuthHeaders(),
            });
            const success = response.data?.success ?? response.data?.Success ?? true;
            setCompetitiveFeedback({
                type: success ? "success" : "error",
                message: response.data?.message || response.data?.Message || (success ? "Perfil competitivo atualizado." : "Nao foi possivel salvar o perfil."),
            });
            if (success) await loadDashboardData();
        } catch (requestError) {
            setCompetitiveFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel salvar o perfil competitivo.",
            });
        } finally {
            setCompetitiveLoading(false);
        }
    }

    const content = (() => {
        if (loading) {
            return <div className="dashboard-state">Carregando dados...</div>;
        }

        if (error) {
            return <div className="dashboard-state dashboard-state-error">{error}</div>;
        }

        if (!dashboardData) {
            return <div className="dashboard-state">Nenhum dado encontrado.</div>;
        }

        return (
            <>
                <div className="dashboard-top-grid">
                    <UserHeader
                        user={dashboardData.user}
                        canEdit={Boolean(currentUser)}
                        loading={profileLoading}
                        feedback={profileFeedback}
                        pendingPreview={profileImagePreview}
                        hasPendingImage={Boolean(profileImageFile)}
                        onImageSelect={handleProfileImageSelect}
                        onImageSave={handleProfileImageSave}
                    />
                    <RankCard rank={dashboardData.rank} />
                </div>

                <StatsOverview stats={dashboardData.stats} />

                {captainTeams.length > 0 && (
                    <section className="dashboard-card dashboard-captain-card">
                        <div className="dashboard-card-heading">
                            <h2>Painel do capitao</h2>
                        </div>
                        <div className="captain-panel-grid">
                            <div>
                                <strong>{captainRequests.length}</strong>
                                <span>Solicitacoes pendentes</span>
                            </div>
                            <div>
                                <strong>{captainTeams.reduce((total, team) => total + team.missingRoles.length, 0)}</strong>
                                <span>Lacunas de rota</span>
                            </div>
                            <div>
                                <strong>{captainTeams.map((team) => team.tag).join(", ")}</strong>
                                <span>Times sob gestao</span>
                            </div>
                        </div>
                        <div className="captain-team-list">
                            {captainTeams.map((team) => (
                                <article key={team.id}>
                                    <strong>{team.tag} - {team.name}</strong>
                                    <span>{team.players}/5 jogadores</span>
                                    <em>{team.missingRoles.length ? `Faltam: ${team.missingRoles.join(", ")}` : "Composicao completa"}</em>
                                </article>
                            ))}
                        </div>
                    </section>
                )}

                <section className="dashboard-card dashboard-competitive-card">
                    <div className="dashboard-card-heading">
                        <h2>Perfil competitivo</h2>
                        <span>Como outros times enxergam seu jogador.</span>
                    </div>
                    <form
                        className="competitive-profile-form"
                        onSubmit={(event) => {
                            event.preventDefault();
                            const form = event.currentTarget;
                            const selectedTags = PLAYER_TAGS.filter((tag) => form.elements[`tag-${tag}`]?.checked);
                            handleCompetitiveProfileSave({
                                mainRole: form.elements.mainRole.value,
                                lookingForTeam: form.elements.lookingForTeam.checked,
                                tags: selectedTags,
                            });
                        }}
                    >
                        <div className="competitive-controls">
                            <label className="competitive-control-card">
                                Rota principal
                                <select name="mainRole" defaultValue={dashboardData.competitive.mainRole}>
                                    {ROLE_OPTIONS.map((role) => <option key={role} value={role}>{role}</option>)}
                                </select>
                            </label>
                            <label className="competitive-toggle competitive-control-card">
                                <input name="lookingForTeam" type="checkbox" defaultChecked={dashboardData.competitive.lookingForTeam} />
                                <span>
                                    <strong>Disponivel para convites</strong>
                                    <small>Aparece como procurando time.</small>
                                </span>
                            </label>
                            <fieldset>
                                <legend>Tags do jogador</legend>
                                <div className="competitive-tag-grid">
                                    {PLAYER_TAGS.map((tag) => (
                                        <label key={tag} className="competitive-tag">
                                            <input name={`tag-${tag}`} type="checkbox" defaultChecked={dashboardData.competitive.tags.includes(tag)} />
                                            <span>{tag}</span>
                                        </label>
                                    ))}
                                </div>
                            </fieldset>
                            <div className="competitive-actions">
                                <button className="dashboard-small-action" type="submit" disabled={competitiveLoading}>
                                    {competitiveLoading ? "Salvando..." : "Salvar perfil"}
                                </button>
                            </div>
                        </div>
                        {competitiveFeedback.message && (
                            <div className={`dashboard-feedback compact ${competitiveFeedback.type}`}>{competitiveFeedback.message}</div>
                        )}
                    </form>
                </section>

                <div className="dashboard-bottom-grid">
                    <ChampionStats champions={dashboardData.champions} />
                    <RoleStats roles={dashboardData.roles} />
                </div>

                <PlayerHistory
                    recentMatches={dashboardData.history.recentMatches}
                    champions={dashboardData.champions}
                    eloHistory={dashboardData.history.eloHistory}
                />
            </>
        );
    })();

    return (
        <main className="dashboard-page">
            <div className="dashboard-bg-grid" />
            <div className="dashboard-container">
                <div className="dashboard-page-heading">
                    <p className="dashboard-eyebrow">Dashboard</p>
                    <h1>Resumo competitivo</h1>
                </div>

                {currentUser && !loading && !dashboardData && (
                    <form className="dashboard-riot-link" onSubmit={handleSyncRiotPlayer}>
                        <label>
                            Vincular Riot ID
                            <input
                                type="text"
                                value={summonerName}
                                onChange={(event) => setSummonerName(event.target.value)}
                                placeholder="Nome#TAG, ex: Thiago#BR1"
                            />
                        </label>
                        <button className="btn-primary" type="submit" disabled={syncLoading}>
                            {syncLoading ? "Vinculando..." : "Sincronizar Riot ID"}
                        </button>
                        {syncFeedback.message && (
                            <div className={`dashboard-feedback ${syncFeedback.type}`}>{syncFeedback.message}</div>
                        )}
                    </form>
                )}

                {content}
            </div>
        </main>
    );
}
