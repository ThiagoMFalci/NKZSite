import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import CreateTournamentModal from "./components/CreateTournamentModal";
import TournamentDetails from "./components/TournamentDetails";
import TournamentFilter from "./components/TournamentFilter";
import TournamentList from "./components/TournamentList";
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

function normalizeTournament(tournament) {
    const teams = (tournament.teams ?? tournament.Teams ?? []).map(normalizeTeam);
    const maxTeams = tournament.maxTeams ?? tournament.MaxTeams ?? 0;
    const teamCount = teams.length;
    const entryFee = tournament.entryFee ?? tournament.EntryFee ?? 0;

    return {
        id: tournament.id ?? tournament.Id,
        name: tournament.name ?? tournament.Name ?? "Campeonato",
        prizeLabel: money(tournament.prize ?? tournament.Prize),
        entryFee,
        entryFeeLabel: entryFee > 0 ? money(entryFee) : "Gratis",
        maxTeams,
        teamCount,
        teams,
        ownerId: tournament.ownerId ?? tournament.OwnerId,
        averageElo: tournament.averageElo ?? tournament.AverageElo ?? getAverageEloFromTeams(teams),
        status: maxTeams && teamCount >= maxTeams ? "Lotado" : "Aberto",
    };
}

export default function TournamentsPage() {
    const [tournaments, setTournaments] = useState([]);
    const [selectedTournament, setSelectedTournament] = useState(null);
    const [createOpen, setCreateOpen] = useState(false);
    const [createForm, setCreateForm] = useState({ name: "", maxTeams: "8", prize: "0", entryFee: "0", joinOwnerTeam: false });
    const [search, setSearch] = useState("");
    const [fee, setFee] = useState("Todos");
    const [selectedElos, setSelectedElos] = useState([]);
    const [eloSort, setEloSort] = useState("none");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [joinLoading, setJoinLoading] = useState(false);
    const [feedback, setFeedback] = useState({ type: "", message: "" });
    const [createLoading, setCreateLoading] = useState(false);
    const [createFeedback, setCreateFeedback] = useState({ type: "", message: "" });
    const [ownedTeam, setOwnedTeam] = useState(null);
    const currentUser = getCurrentUser();

    async function loadTournaments(isMounted = () => true) {
        try {
            setLoading(true);
            setError("");
            const response = await axios.get(`${API_BASE_URL}/api/Tournament`, { headers: getAuthHeaders() });
            const data = unwrapApiData(response.data);
            if (isMounted()) setTournaments((Array.isArray(data) ? data : []).map(normalizeTournament));
        } catch (requestError) {
            if (isMounted()) setError(requestError?.response?.data?.message || "Erro ao carregar campeonatos.");
        } finally {
            if (isMounted()) setLoading(false);
        }
    }

    useEffect(() => {
        let isMounted = true;

        loadTournaments(() => isMounted);

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
    }, [currentUser?.userId]);

    const filteredTournaments = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();
        const filtered = tournaments.filter((tournament) => {
            const matchesName = tournament.name.toLowerCase().includes(normalizedSearch);
            const matchesFee =
                fee === "Todos" ||
                (fee === "Gratis" && tournament.entryFee === 0) ||
                (fee === "Pago" && tournament.entryFee > 0);
            const matchesElo = matchesSelectedElos(tournament.averageElo, selectedElos);
            return matchesName && matchesFee && matchesElo;
        });
        return sortByElo(filtered, eloSort, (tournament) => tournament.averageElo);
    }, [eloSort, fee, search, selectedElos, tournaments]);

    function toggleElo(elo) {
        setSelectedElos((current) =>
            current.includes(elo) ? current.filter((item) => item !== elo) : [...current, elo]
        );
    }

    async function handleSelectTournament(tournament) {
        setFeedback({ type: "", message: "" });
        try {
            const response = await axios.get(`${API_BASE_URL}/api/Tournament/${tournament.id}`, { headers: getAuthHeaders() });
            setSelectedTournament(normalizeTournament(unwrapApiData(response.data)));
        } catch {
            setSelectedTournament(tournament);
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

    async function refreshSelectedTournament(tournamentId = selectedTournament?.id) {
        if (!tournamentId) return;
        try {
            const response = await axios.get(`${API_BASE_URL}/api/Tournament/${tournamentId}`, { headers: getAuthHeaders() });
            const updated = normalizeTournament(unwrapApiData(response.data));
            setSelectedTournament(updated);
            setTournaments((current) => current.map((tournament) => tournament.id === updated.id ? updated : tournament));
        } catch {
            await loadTournaments();
        }
    }

    async function handleJoinTournament() {
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

            const response = await axios.post(`${API_BASE_URL}/api/Tournament/${selectedTournament.id}/teams/${teamId}`, null, {
                headers: getAuthHeaders(),
            });

            setFeedback({
                type: "success",
                message: response.data?.message || response.data?.Message || "Time inscrito no campeonato.",
            });
            await refreshSelectedTournament();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel inscrever o time.",
            });
        } finally {
            setJoinLoading(false);
        }
    }

    async function handleLeaveTournament(teamId = ownedTeam?.id ?? ownedTeam?.Id) {
        if (!teamId) {
            setFeedback({ type: "error", message: "Voce precisa ser dono de um time para sair." });
            return;
        }

        if (!window.confirm("Tem certeza que deseja tirar este time do campeonato?")) return;

        try {
            setJoinLoading(true);
            setFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/Tournament/${selectedTournament.id}/teams/${teamId}`, {
                headers: getAuthHeaders(),
            });
            setFeedback({ type: "success", message: "Time removido do campeonato." });
            await refreshSelectedTournament();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel remover o time.",
            });
        } finally {
            setJoinLoading(false);
        }
    }

    async function handleDeleteTournament() {
        if (!selectedTournament?.id) return;
        if (!window.confirm("Tem certeza que deseja excluir este campeonato? Essa acao nao pode ser desfeita.")) return;

        try {
            setJoinLoading(true);
            setFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/Tournament/${selectedTournament.id}`, {
                headers: getAuthHeaders(),
            });
            setTournaments((current) => current.filter((tournament) => tournament.id !== selectedTournament.id));
            setSelectedTournament(null);
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel excluir o campeonato.",
            });
        } finally {
            setJoinLoading(false);
        }
    }

    function handleCreateChange(event) {
        const { name, value, type, checked } = event.target;
        setCreateForm((current) => ({ ...current, [name]: type === "checkbox" ? checked : value }));
    }

    async function handleCreateTournament(event) {
        event.preventDefault();
        const user = getCurrentUser();

        if (!user?.userId) {
            setCreateFeedback({ type: "error", message: "Entre na sua conta para criar um campeonato." });
            return;
        }

        if (!createForm.name.trim()) {
            setCreateFeedback({ type: "error", message: "Informe o nome do campeonato." });
            return;
        }

        try {
            setCreateLoading(true);
            setCreateFeedback({ type: "", message: "" });

            const payload = {
                name: createForm.name.trim(),
                maxTeams: Number(createForm.maxTeams) || 2,
                prize: Number(createForm.prize) || 0,
                entryFee: Number(createForm.entryFee) || 0,
                ownerId: user.userId,
            };

            const response = await axios.post(`${API_BASE_URL}/api/Tournament`, payload, {
                headers: getAuthHeaders(),
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            if (!success) {
                setCreateFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Nao foi possivel criar o campeonato.",
                });
                return;
            }

            const newTournamentId = response.data?.data ?? response.data?.Data;
            if (createForm.joinOwnerTeam && newTournamentId) {
                const teamId = await getOwnedTeamId(user.userId);
                if (teamId) {
                    await axios.post(`${API_BASE_URL}/api/Tournament/${newTournamentId}/teams/${teamId}`, null, {
                        headers: getAuthHeaders(),
                    });
                }
            }
            setTournaments((current) => [
                normalizeTournament({ ...payload, id: newTournamentId, teams: [] }),
                ...current,
            ]);
            setCreateForm({ name: "", maxTeams: "8", prize: "0", entryFee: "0", joinOwnerTeam: false });
            setCreateFeedback({ type: "success", message: "Campeonato criado com sucesso." });
        } catch (requestError) {
            const status = requestError?.response?.status;
            setCreateFeedback({
                type: "error",
                message: status === 403
                    ? "Sua conta nao tem permissao de administrador para criar campeonatos."
                    : requestError?.response?.data?.message || "Nao foi possivel criar o campeonato.",
            });
        } finally {
            setCreateLoading(false);
        }
    }

    return (
        <main className="tournaments-page">
            <div className="tournaments-bg-grid" />
            <div className="tournaments-container">
                <div className="tournaments-heading">
                    <div>
                        <p className="tournaments-eyebrow">Campeonatos</p>
                        <h1>Chaves e disputas</h1>
                    </div>
                    <span>{filteredTournaments.length} campeonatos</span>
                </div>

                {currentUser && (
                    <div className="tournaments-actions">
                        <button className="btn-primary" onClick={() => setCreateOpen(true)}>
                            Criar campeonato
                        </button>
                    </div>
                )}

                <TournamentFilter
                    search={search}
                    fee={fee}
                    selectedElos={selectedElos}
                    eloSort={eloSort}
                    onSearchChange={setSearch}
                    onFeeChange={setFee}
                    onEloToggle={toggleElo}
                    onEloSortChange={setEloSort}
                />

                {loading && <section className="tournaments-state">Carregando campeonatos...</section>}
                {error && !loading && <section className="tournaments-state tournaments-state-error">{error}</section>}
                {!loading && !error && <TournamentList tournaments={filteredTournaments} onSelect={handleSelectTournament} />}
            </div>

            <TournamentDetails
                tournament={selectedTournament}
                loading={joinLoading}
                feedback={feedback}
                currentUser={currentUser}
                ownedTeam={ownedTeam ? normalizeTeam(ownedTeam) : null}
                onClose={() => setSelectedTournament(null)}
                onJoin={handleJoinTournament}
                onLeave={handleLeaveTournament}
                onDelete={handleDeleteTournament}
            />

            <CreateTournamentModal
                open={createOpen}
                formData={createForm}
                loading={createLoading}
                feedback={createFeedback}
                onClose={() => setCreateOpen(false)}
                onChange={handleCreateChange}
                onSubmit={handleCreateTournament}
            />
        </main>
    );
}
