import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import LeagueDetails from "./components/LeagueDetails";
import LeagueFilter from "./components/LeagueFilter";
import LeagueList from "./components/LeagueList";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import { ELO_SCORE, matchesSelectedElos, normalizeEloLabel, sortByElo } from "../../utils/elo";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? [];
}

function money(value) {
    return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function getAverageEloFromPlayers(players = []) {
    const tiers = players
        .map((player) => String(player.soloQueueTier ?? player.SoloQueueTier ?? "").toUpperCase())
        .filter((tier) => ELO_SCORE[tier]);
    if (!tiers.length) return "Unranked";
    const average = tiers.reduce((total, tier) => total + ELO_SCORE[tier], 0) / tiers.length;
    const [closestTier] = Object.entries(ELO_SCORE).reduce((closest, current) =>
        Math.abs(current[1] - average) < Math.abs(closest[1] - average) ? current : closest
    );
    return normalizeEloLabel(closestTier);
}

function normalizeTeam(team) {
    const players = team.players ?? team.Players ?? [];
    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        playerCount: players.length,
        ownerId: team.ownerId ?? team.OwnerId,
        averageElo: team.averageElo ?? team.AverageElo ?? getAverageEloFromPlayers(players),
    };
}

function getAverageEloFromTeams(teams = []) {
    const scores = teams.map((team) => ELO_SCORE[String(team.averageElo || "").toUpperCase()]).filter(Boolean);
    if (!scores.length) return "Unranked";
    const average = scores.reduce((total, score) => total + score, 0) / scores.length;
    const [closestTier] = Object.entries(ELO_SCORE).reduce((closest, current) =>
        Math.abs(current[1] - average) < Math.abs(closest[1] - average) ? current : closest
    );
    return normalizeEloLabel(closestTier);
}

function normalizeLeague(league) {
    const teams = (league.teams ?? league.Teams ?? []).map(normalizeTeam);
    const maxTeams = league.maxTeams ?? league.MaxTeams ?? 0;
    const teamCount = teams.length;

    return {
        id: league.id ?? league.Id,
        name: league.name ?? league.Name ?? "Liga",
        award: league.award ?? league.Award ?? 0,
        awardLabel: money(league.award ?? league.Award),
        entryFeeLabel: money(league.entryFee ?? league.EntryFee),
        maxTeams,
        teamCount,
        teams,
        averageElo: league.averageElo ?? league.AverageElo ?? getAverageEloFromTeams(teams),
        occupancy: maxTeams ? Math.min(100, Math.round((teamCount / maxTeams) * 100)) : 0,
        status: maxTeams && teamCount >= maxTeams ? "Lotada" : "Aberta",
    };
}

export default function LeaguesPage() {
    const [leagues, setLeagues] = useState([]);
    const [selectedLeague, setSelectedLeague] = useState(null);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("Todas");
    const [awardFilter, setAwardFilter] = useState("Todas");
    const [selectedElos, setSelectedElos] = useState([]);
    const [eloSort, setEloSort] = useState("none");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [joinLoading, setJoinLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });
    const [ownedTeam, setOwnedTeam] = useState(null);

    async function loadLeagues(isMounted = () => true) {
        try {
            setLoading(true);
            setError("");
            const response = await axios.get(`${API_BASE_URL}/api/league/ListLeagues`);
            const data = unwrapApiData(response.data);
            if (isMounted()) setLeagues((Array.isArray(data) ? data : []).map(normalizeLeague));
        } catch (requestError) {
            if (isMounted()) setError(requestError?.response?.data?.message || "Erro ao carregar ligas.");
        } finally {
            if (isMounted()) setLoading(false);
        }
    }

    useEffect(() => {
        let isMounted = true;
        const currentUser = getCurrentUser();

        loadLeagues(() => isMounted);
        if (currentUser?.userId) {
            getOwnedTeam(currentUser.userId)
                .then((team) => {
                    if (isMounted) setOwnedTeam(team);
                })
                .catch(() => {
                    if (isMounted) setOwnedTeam(null);
                });
        }

        return () => {
            isMounted = false;
        };
    }, []);

    const filteredLeagues = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();
        const filtered = leagues.filter((league) => {
            const matchesName = league.name.toLowerCase().includes(normalizedSearch);
            const matchesStatus = status === "Todas" || league.status === status;
            const matchesAward =
                awardFilter === "Todas" ||
                (awardFilter === "Sem premio" && league.award === 0) ||
                (awardFilter === "Ate 100" && league.award > 0 && league.award <= 100) ||
                (awardFilter === "Acima de 100" && league.award > 100);
            const matchesElo = matchesSelectedElos(league.averageElo, selectedElos);
            return matchesName && matchesStatus && matchesAward && matchesElo;
        });
        return sortByElo(filtered, eloSort, (league) => league.averageElo);
    }, [awardFilter, eloSort, leagues, search, selectedElos, status]);

    function toggleElo(elo) {
        setSelectedElos((current) =>
            current.includes(elo) ? current.filter((item) => item !== elo) : [...current, elo]
        );
    }

    async function handleSelectLeague(league) {
        setFeedback({ type: "", message: "" });
        try {
            const response = await axios.get(`${API_BASE_URL}/api/league/${league.id}`);
            setSelectedLeague(normalizeLeague(unwrapApiData(response.data)));
        } catch {
            setSelectedLeague(league);
        }
    }

    async function getOwnedTeam(userId) {
        const response = await axios.get(`${API_BASE_URL}/api/team/ListTeams`, { headers: getAuthHeaders() });
        const teams = unwrapApiData(response.data);
        return (Array.isArray(teams) ? teams : []).find((team) => (team.ownerId ?? team.OwnerId) === userId) || null;
    }

    async function getOwnedTeamId(userId) {
        const team = ownedTeam?.id || ownedTeam?.Id ? ownedTeam : await getOwnedTeam(userId);
        return team?.id ?? team?.Id;
    }

    async function refreshSelectedLeague(leagueId = selectedLeague?.id) {
        if (!leagueId) return;
        try {
            const response = await axios.get(`${API_BASE_URL}/api/league/${leagueId}`);
            const updated = normalizeLeague(unwrapApiData(response.data));
            setSelectedLeague(updated);
            setLeagues((current) => current.map((league) => league.id === updated.id ? updated : league));
        } catch {
            await loadLeagues();
        }
    }

    async function handleJoinLeague() {
        const user = getCurrentUser();
        if (!user?.userId) {
            setFeedback({ type: "error", message: "Entre na sua conta para inscrever um time." });
            return;
        }

        try {
            setJoinLoading(true);
            setFeedback({ type: "", message: "" });
            const teamId = await getOwnedTeamId(user.userId);

            if (!teamId) {
                setFeedback({ type: "error", message: "Voce precisa ser dono de um time para se inscrever." });
                return;
            }

            const response = await axios.post(`${API_BASE_URL}/api/league/${selectedLeague.id}/teams/${teamId}`, null, {
                headers: getAuthHeaders(),
            });

            setFeedback({
                type: "success",
                message: response.data?.message || response.data?.Message || "Time inscrito na liga.",
            });
            await refreshSelectedLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel inscrever o time.",
            });
        } finally {
            setJoinLoading(false);
        }
    }

    async function handleLeaveLeague() {
        const user = getCurrentUser();
        const teamId = user?.userId ? await getOwnedTeamId(user.userId) : null;
        if (!teamId) {
            setFeedback({ type: "error", message: "Voce precisa ser dono de um time para sair." });
            return;
        }

        if (!window.confirm("Tem certeza que deseja sair desta liga?")) return;

        try {
            setJoinLoading(true);
            setFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/league/${selectedLeague.id}/teams/${teamId}`, {
                headers: getAuthHeaders(),
            });
            setFeedback({ type: "success", message: "Time removido da liga." });
            await refreshSelectedLeague();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel sair da liga.",
            });
        } finally {
            setJoinLoading(false);
        }
    }

    return (
        <main className="leagues-page">
            <div className="leagues-bg-grid" />
            <div className="leagues-container">
                <div className="leagues-heading">
                    <div>
                        <p className="leagues-eyebrow">Ligas</p>
                        <h1>Temporadas competitivas</h1>
                    </div>
                    <span>{filteredLeagues.length} ligas</span>
                </div>

                <LeagueFilter
                    search={search}
                    status={status}
                    awardFilter={awardFilter}
                    selectedElos={selectedElos}
                    eloSort={eloSort}
                    onSearchChange={setSearch}
                    onStatusChange={setStatus}
                    onAwardFilterChange={setAwardFilter}
                    onEloToggle={toggleElo}
                    onEloSortChange={setEloSort}
                />

                {loading && <section className="leagues-state">Carregando ligas...</section>}
                {error && !loading && <section className="leagues-state leagues-state-error">{error}</section>}
                {!loading && !error && <LeagueList leagues={filteredLeagues} onSelect={handleSelectLeague} />}
            </div>

            <LeagueDetails
                league={selectedLeague}
                loading={joinLoading}
                feedback={feedback}
                ownedTeam={ownedTeam ? normalizeTeam(ownedTeam) : null}
                onClose={() => setSelectedLeague(null)}
                onJoin={handleJoinLeague}
                onLeave={handleLeaveLeague}
            />
        </main>
    );
}
