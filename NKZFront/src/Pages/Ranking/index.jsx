import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import RankingFilter from "./components/RankingFilter";
import RankingTable from "./components/RankingTable";
import { matchesSelectedElos, normalizeEloLabel, sortByElo } from "../../utils/elo";
import { getPlayerImageUrl } from "../../utils/images";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

const tierScore = {
    IRON: 100,
    BRONZE: 200,
    SILVER: 300,
    GOLD: 400,
    PLATINUM: 500,
    EMERALD: 600,
    DIAMOND: 700,
    MASTER: 850,
    GRANDMASTER: 950,
    CHALLENGER: 1100,
};

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? [];
}

function normalizeElo(tier, rank) {
    const normalizedTier = String(tier || "Unranked");
    const label = normalizedTier.charAt(0).toUpperCase() + normalizedTier.slice(1).toLowerCase();
    return `${label} ${rank || ""}`.trim();
}

function calculateWinRate(wins, losses) {
    const total = wins + losses;
    return total ? Math.round((wins / total) * 100) : 0;
}

function normalizePlayer(player) {
    const tier = player.soloQueueTier ?? player.SoloQueueTier ?? "UNRANKED";
    const rank = player.soloQueueRank ?? player.SoloQueueRank ?? "";
    const lp = player.soloQueueLP ?? player.SoloQueueLP ?? 0;
    const wins = player.wins ?? player.Wins ?? 0;
    const losses = player.losses ?? player.Losses ?? 0;
    const totalMatches = player.totalMatches ?? player.TotalMatches ?? wins + losses;
    const points = (tierScore[String(tier).toUpperCase()] || 0) + lp + wins * 3 - losses;

    return {
        id: player.id ?? player.Id,
        summonerName: player.summonerName ?? player.SummonerName ?? "Invocador",
        profileImageUrl: getPlayerImageUrl(player),
        summonerLevel: player.summonerLevel ?? player.SummonerLevel ?? 0,
        tier,
        elo: normalizeElo(tier, rank),
        totalMatches,
        winRate: calculateWinRate(wins, losses),
        points: Math.max(0, points),
    };
}

function getAverageElo(players) {
    const rankedPlayers = players
        .map((player) => String(player.soloQueueTier ?? player.SoloQueueTier ?? "").toUpperCase())
        .filter((tier) => tierScore[tier]);

    if (!rankedPlayers.length) return "Unranked";

    const averageScore = rankedPlayers.reduce((total, tier) => total + tierScore[tier], 0) / rankedPlayers.length;
    const [closestTier] = Object.entries(tierScore).reduce((closest, current) => {
        return Math.abs(current[1] - averageScore) < Math.abs(closest[1] - averageScore) ? current : closest;
    });

    return normalizeEloLabel(closestTier);
}

function normalizeTeam(team) {
    const players = team.players ?? team.Players ?? [];
    const wins = team.wins ?? team.Wins ?? 0;
    const points = team.points ?? team.Points ?? players.reduce((total, player) => {
        const playerWins = player.wins ?? player.Wins ?? 0;
        const playerLosses = player.losses ?? player.Losses ?? 0;
        return total + Math.max(0, playerWins * 3 - playerLosses);
    }, 0);

    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        tag: team.tag ?? team.Tag ?? "TIME",
        tier: getAverageElo(players),
        elo: getAverageElo(players),
        players: players.length,
        wins,
        points,
    };
}

function normalizeTournamentWins(tournaments) {
    const winsByTeam = new Map();

    (Array.isArray(tournaments) ? tournaments : []).forEach((tournament) => {
        const winner = tournament.winnerTeam ?? tournament.WinnerTeam ?? tournament.championTeam ?? tournament.ChampionTeam;
        const winnerId = tournament.winnerTeamId ?? tournament.WinnerTeamId ?? winner?.id ?? winner?.Id;
        if (!winnerId) return;

        const current = winsByTeam.get(winnerId) || {
            id: winnerId,
            name: winner?.name ?? winner?.Name ?? "Time campeao",
            tag: winner?.tag ?? winner?.Tag ?? "TIME",
            wins: 0,
            lastTournament: "",
            points: 0,
        };

        current.wins += 1;
        current.points = current.wins;
        current.lastTournament = tournament.name ?? tournament.Name ?? current.lastTournament;
        winsByTeam.set(winnerId, current);
    });

    return [...winsByTeam.values()].sort((a, b) => b.wins - a.wins);
}

export default function RankingPage() {
    const [players, setPlayers] = useState([]);
    const [teams, setTeams] = useState([]);
    const [tournamentWins, setTournamentWins] = useState([]);
    const [activeTab, setActiveTab] = useState("players");
    const [search, setSearch] = useState("");
    const [selectedElos, setSelectedElos] = useState([]);
    const [eloSort, setEloSort] = useState("none");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let isMounted = true;

        async function loadRanking() {
            try {
                setLoading(true);
                setError("");
                const [playersResponse, teamsResponse, tournamentsResponse] = await Promise.all([
                    axios.get(`${API_BASE_URL}/api/player`),
                    axios.get(`${API_BASE_URL}/api/team/ListTeams`).catch(() => ({ data: [] })),
                    axios.get(`${API_BASE_URL}/api/Tournament`).catch(() => ({ data: [] })),
                ]);
                const data = unwrapApiData(playersResponse.data);

                if (isMounted) {
                    setPlayers((Array.isArray(data) ? data : []).map(normalizePlayer));
                    setTeams((Array.isArray(unwrapApiData(teamsResponse.data)) ? unwrapApiData(teamsResponse.data) : []).map(normalizeTeam));
                    setTournamentWins(normalizeTournamentWins(unwrapApiData(tournamentsResponse.data)));
                }
            } catch (requestError) {
                if (isMounted) {
                    setError(requestError?.response?.data?.message || "Erro ao carregar ranking.");
                }
            } finally {
                if (isMounted) setLoading(false);
            }
        }

        loadRanking();

        return () => {
            isMounted = false;
        };
    }, []);

    const filteredPlayers = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();

        const filtered = players
            .filter((player) => {
                const matchesName = player.summonerName.toLowerCase().includes(normalizedSearch);
                const matchesElo = matchesSelectedElos(player.tier, selectedElos);
                return matchesName && matchesElo;
            })
            .sort((a, b) => b.points - a.points);

        return sortByElo(filtered, eloSort, (player) => player.tier);
    }, [eloSort, players, search, selectedElos]);

    const filteredTeams = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();
        const filtered = teams
            .filter((team) => {
                const matchesName = team.name.toLowerCase().includes(normalizedSearch) || team.tag.toLowerCase().includes(normalizedSearch);
                const matchesElo = matchesSelectedElos(team.elo, selectedElos);
                return matchesName && matchesElo;
            })
            .sort((a, b) => b.points - a.points);

        return sortByElo(filtered, eloSort, (team) => team.elo);
    }, [eloSort, search, selectedElos, teams]);

    const filteredTournamentWins = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();
        return tournamentWins.filter((team) => (
            team.name.toLowerCase().includes(normalizedSearch) || team.tag.toLowerCase().includes(normalizedSearch)
        ));
    }, [search, tournamentWins]);

    const currentRows = activeTab === "teams"
        ? filteredTeams
        : activeTab === "tournaments"
            ? filteredTournamentWins
            : filteredPlayers;

    const heading = {
        players: ["Melhores jogadores", `${filteredPlayers.length} jogadores`],
        teams: ["Melhores times", `${filteredTeams.length} times`],
        tournaments: ["Campeonatos vencidos", `${filteredTournamentWins.length} times campeoes`],
    }[activeTab];

    function toggleElo(elo) {
        setSelectedElos((current) =>
            current.includes(elo) ? current.filter((item) => item !== elo) : [...current, elo]
        );
    }

    return (
        <main className="ranking-page">
            <div className="ranking-bg-grid" />
            <div className="ranking-container">
                <div className="ranking-heading">
                    <div>
                        <p className="ranking-eyebrow">Ranking</p>
                        <h1>{heading[0]}</h1>
                    </div>
                    <span>{heading[1]}</span>
                </div>

                <div className="ranking-tabs" role="tablist" aria-label="Tipo de ranking">
                    <button className={activeTab === "players" ? "active" : ""} onClick={() => setActiveTab("players")} type="button">
                        Jogadores
                    </button>
                    <button className={activeTab === "teams" ? "active" : ""} onClick={() => setActiveTab("teams")} type="button">
                        Times
                    </button>
                    <button className={activeTab === "tournaments" ? "active" : ""} onClick={() => setActiveTab("tournaments")} type="button">
                        Campeonatos vencidos
                    </button>
                </div>

                {activeTab !== "tournaments" && (
                    <RankingFilter
                        search={search}
                        selectedElos={selectedElos}
                        eloSort={eloSort}
                        onSearchChange={setSearch}
                        onEloToggle={toggleElo}
                        onEloSortChange={setEloSort}
                        searchLabel={activeTab === "teams" ? "Buscar time" : "Buscar jogador"}
                        searchPlaceholder={activeTab === "teams" ? "Nome ou tag do time" : "Nome do invocador"}
                    />
                )}
                {activeTab === "tournaments" && (
                    <section className="ranking-filter compact-filter">
                        <label>
                            Buscar time campeao
                            <input
                                type="search"
                                value={search}
                                onChange={(event) => setSearch(event.target.value)}
                                placeholder="Nome ou tag do time"
                            />
                        </label>
                    </section>
                )}

                {loading && <section className="ranking-state">Carregando ranking...</section>}
                {error && !loading && <section className="ranking-state ranking-state-error">{error}</section>}
                {!loading && !error && <RankingTable rows={currentRows} type={activeTab} />}
            </div>
        </main>
    );
}
