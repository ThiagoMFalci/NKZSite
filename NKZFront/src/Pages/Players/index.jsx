import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { BsController, BsDiscord, BsEnvelopePlus, BsPeopleFill, BsStars } from "react-icons/bs";
import EloSelector from "../../Components/EloSelector";
import RankEmblem from "../../Components/RankEmblem";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import { calculateRankPoints, calculateWinRate, matchesSelectedElos, normalizeEloLabel, sortByElo } from "../../utils/elo";
import { getPlayerImageUrl } from "../../utils/images";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";
const ROLE_OPTIONS = ["Top", "Jungle", "Mid", "ADC", "Support", "Flex"];

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? [];
}

function normalizeChampions(champions = []) {
    return (Array.isArray(champions) ? champions : [])
        .map((champion) => ({
            name: champion.name ?? champion.championName ?? champion.ChampionName ?? "Campeao",
            matches: champion.matches ?? champion.totalMatches ?? champion.TotalMatches ?? 0,
        }))
        .sort((a, b) => b.matches - a.matches)
        .slice(0, 3);
}

function normalizePlayer(player) {
    const tier = player.soloQueueTier ?? player.SoloQueueTier ?? "UNRANKED";
    const rank = player.soloQueueRank ?? player.SoloQueueRank ?? "";
    const lp = player.soloQueueLP ?? player.SoloQueueLP ?? 0;
    const wins = player.wins ?? player.Wins ?? 0;
    const losses = player.losses ?? player.Losses ?? 0;
    const role = player.role ?? player.Role ?? player.mainRole ?? player.MainRole ?? "Flex";
    const tags = String(player.tags ?? player.Tags ?? "")
        .split(",")
        .map((tag) => tag.trim())
        .filter(Boolean);

    return {
        id: player.id ?? player.Id,
        userId: player.userId ?? player.UserId,
        teamId: player.teamId ?? player.TeamId,
        nick: player.summonerName ?? player.SummonerName ?? "Invocador",
        discordUsername: player.discordUsername ?? player.DiscordUsername ?? "",
        profileImageUrl: getPlayerImageUrl(player),
        rank: `${normalizeEloLabel(tier)} ${rank}`.trim(),
        tier,
        role,
        lookingForTeam: player.lookingForTeam ?? player.LookingForTeam ?? true,
        tags,
        points: calculateRankPoints(tier, rank, lp),
        level: player.summonerLevel ?? player.SummonerLevel ?? 0,
        wins,
        losses,
        winRate: calculateWinRate(wins, losses),
        champions: normalizeChampions(player.championStats ?? player.ChampionStats ?? []),
    };
}

function normalizeTeam(team) {
    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        tag: team.tag ?? team.Tag ?? "TIME",
        ownerId: team.ownerId ?? team.OwnerId,
        players: team.players ?? team.Players ?? [],
    };
}

export default function PlayersPage() {
    const [players, setPlayers] = useState([]);
    const [myTeam, setMyTeam] = useState(null);
    const [search, setSearch] = useState("");
    const [selectedElos, setSelectedElos] = useState([]);
    const [roleFilter, setRoleFilter] = useState("all");
    const [availabilityFilter, setAvailabilityFilter] = useState("all");
    const [eloSort, setEloSort] = useState("none");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [inviteLoadingId, setInviteLoadingId] = useState("");
    const [feedback, setFeedback] = useState({ type: "", message: "" });
    const currentUser = getCurrentUser();
    const navigate = useNavigate();

    useEffect(() => {
        let isMounted = true;

        async function loadPlayers() {
            try {
                setLoading(true);
                setError("");

                const [playersResponse, teamsResponse] = await Promise.all([
                    axios.get(`${API_BASE_URL}/api/player`, { headers: getAuthHeaders() }),
                    axios.get(`${API_BASE_URL}/api/team/ListTeams`, { headers: getAuthHeaders() }).catch(() => ({ data: [] })),
                ]);

                if (!isMounted) return;

                const normalizedPlayers = (Array.isArray(unwrapApiData(playersResponse.data)) ? unwrapApiData(playersResponse.data) : []).map(normalizePlayer);
                const teams = (Array.isArray(unwrapApiData(teamsResponse.data)) ? unwrapApiData(teamsResponse.data) : []).map(normalizeTeam);
                const ownedOrCaptainTeam = teams.find((team) => (
                    team.ownerId === currentUser?.userId ||
                    team.players.some((player) => (player.userId ?? player.UserId) === currentUser?.userId && (player.isCaptain ?? player.IsCaptain))
                ));

                setPlayers(normalizedPlayers);
                setMyTeam(ownedOrCaptainTeam || null);
            } catch (requestError) {
                if (isMounted) {
                    setError(requestError?.response?.data?.message || "Erro ao carregar jogadores.");
                }
            } finally {
                if (isMounted) setLoading(false);
            }
        }

        loadPlayers();

        return () => {
            isMounted = false;
        };
    }, [currentUser?.userId]);

    const filteredPlayers = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();
        const filtered = players.filter((player) => {
            const matchesName = player.nick.toLowerCase().includes(normalizedSearch);
            const matchesElo = matchesSelectedElos(player.tier, selectedElos);
            const matchesRole = roleFilter === "all" || player.role.toLowerCase() === roleFilter.toLowerCase();
            const matchesAvailability =
                availabilityFilter === "all" ||
                (availabilityFilter === "without-team" && !player.teamId) ||
                (availabilityFilter === "looking" && player.lookingForTeam);
            return matchesName && matchesElo && matchesRole && matchesAvailability;
        });

        const byPoints = [...filtered].sort((a, b) => b.points - a.points || b.winRate - a.winRate);
        return sortByElo(byPoints, eloSort, (player) => player.tier);
    }, [availabilityFilter, eloSort, players, roleFilter, search, selectedElos]);

    function toggleElo(elo) {
        setSelectedElos((current) =>
            current.includes(elo) ? current.filter((item) => item !== elo) : [...current, elo]
        );
    }

    function canInvite(player) {
        if (!myTeam || !player.id) return false;
        if (player.userId === currentUser?.userId) return false;
        return !myTeam.players.some((member) => (member.id ?? member.Id) === player.id || (member.userId ?? member.UserId) === player.userId);
    }

    async function handleInvite(player) {
        if (!myTeam) {
            setFeedback({ type: "error", message: "Voce precisa ter um time para convidar jogadores." });
            return;
        }

        try {
            setInviteLoadingId(player.id);
            setFeedback({ type: "", message: "" });

            const invitation = {
                teamId: myTeam.id,
                playerId: player.id,
                senderId: currentUser.userId,
                type: "Invite",
                status: "Pending",
            };

            const response = await axios.post(`${API_BASE_URL}/api/team/${myTeam.id}/invitations`, invitation, {
                headers: getAuthHeaders(),
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            setFeedback({
                type: success ? "success" : "error",
                message: response.data?.message || response.data?.Message || (success ? `Convite enviado para ${player.nick}.` : "Nao foi possivel enviar o convite."),
            });
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel enviar o convite.",
            });
        } finally {
            setInviteLoadingId("");
        }
    }

    return (
        <main className="players-page">
            <div className="players-bg-grid" />
            <div className="players-container">
                <div className="players-heading">
                    <div>
                        <p className="players-eyebrow">Jogadores</p>
                        <h1>Mercado de talentos</h1>
                    </div>
                    <span>{filteredPlayers.length} jogadores</span>
                </div>

                <section className="players-filter filter-shell">
                    <label className="filter-control">
                        Buscar jogador
                        <input
                            type="search"
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                            placeholder="Nome do invocador"
                        />
                    </label>

                    <EloSelector label="Rank" selectedElos={selectedElos} onToggle={toggleElo} />

                    <label className="filter-control">
                        Rota
                        <select value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)}>
                            <option value="all">Todas</option>
                            {ROLE_OPTIONS.map((role) => <option key={role} value={role}>{role}</option>)}
                        </select>
                    </label>

                    <label className="filter-control">
                        Status
                        <select value={availabilityFilter} onChange={(event) => setAvailabilityFilter(event.target.value)}>
                            <option value="all">Todos</option>
                            <option value="looking">Procurando time</option>
                            <option value="without-team">Sem time</option>
                        </select>
                    </label>

                    <label className="filter-control">
                        Ordem de elo
                        <select value={eloSort} onChange={(event) => setEloSort(event.target.value)}>
                            <option value="none">Pontos</option>
                            <option value="asc">Crescente</option>
                            <option value="desc">Decrescente</option>
                        </select>
                    </label>
                </section>

                {feedback.message && <div className={`players-feedback ${feedback.type}`}>{feedback.message}</div>}
                {loading && <section className="players-state">Carregando jogadores...</section>}
                {error && !loading && <section className="players-state players-state-error">{error}</section>}

                {!loading && !error && (
                    <section className="players-grid">
                        {filteredPlayers.length ? filteredPlayers.map((player) => (
                            <article
                                key={player.id}
                                className="player-card"
                                onClick={() => navigate(`/players/${player.id}`)}
                                role="button"
                                tabIndex={0}
                                onKeyDown={(event) => {
                                    if (event.key === "Enter" || event.key === " ") navigate(`/players/${player.id}`);
                                }}
                            >
                                <div className="player-card-top">
                                    <div className="player-avatar">
                                        <span>{player.nick.slice(0, 2).toUpperCase()}</span>
                                        {player.profileImageUrl && (
                                            <img
                                                src={player.profileImageUrl}
                                                alt={player.nick}
                                                onError={(event) => {
                                                    event.currentTarget.style.display = "none";
                                                }}
                                            />
                                        )}
                                    </div>
                                    <div>
                                        <p className="players-eyebrow">{player.role}</p>
                                        <h2>{player.nick}</h2>
                                        <span>Nivel {player.level}</span>
                                        {player.discordUsername && (
                                            <span className="player-discord-tag"><BsDiscord /> {player.discordUsername}</span>
                                        )}
                                    </div>
                                    <em className={`player-looking-badge ${player.lookingForTeam ? "active" : ""}`}>
                                        {player.lookingForTeam ? "Procurando time" : "Fechado"}
                                    </em>
                                </div>

                                <div className="player-card-stats">
                                    <span><RankEmblem tier={player.tier} label={player.rank} className="compact" /> {player.rank}</span>
                                    <span><BsStars /> {player.points} pontos</span>
                                    <span><BsController /> {player.winRate}% win rate</span>
                                </div>

                                <div className="player-champions">
                                    <strong>{player.tags.length ? "Tags" : "Campeoes mais usados"}</strong>
                                    {(player.tags.length || player.champions.length) ? (
                                        <div className="player-champion-list">
                                            {(player.tags.length ? player.tags : player.champions.map((champion) => champion.name)).map((item) => (
                                                <span key={item}>{item}</span>
                                            ))}
                                        </div>
                                    ) : (
                                        <small>Sem dados sincronizados</small>
                                    )}
                                </div>

                                <button
                                    className="player-invite-button"
                                    type="button"
                                    disabled={!canInvite(player) || inviteLoadingId === player.id}
                                    onClick={(event) => {
                                        event.stopPropagation();
                                        handleInvite(player);
                                    }}
                                >
                                    <BsEnvelopePlus />
                                    {inviteLoadingId === player.id ? "Enviando..." : myTeam ? "Convidar para meu time" : "Crie um time para convidar"}
                                </button>
                            </article>
                        )) : (
                            <section className="players-state">Nenhum jogador encontrado.</section>
                        )}
                    </section>
                )}

                {myTeam && (
                    <aside className="players-team-note">
                        <BsPeopleFill />
                        Convites serao enviados pelo time {myTeam.tag} - {myTeam.name}.
                    </aside>
                )}
            </div>
        </main>
    );
}
