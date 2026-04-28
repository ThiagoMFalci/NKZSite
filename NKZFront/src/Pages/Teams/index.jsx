import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import CreateTeamModal from "./components/CreateTeamModal";
import TeamDetails from "./components/TeamDetails";
import TeamFilter from "./components/TeamFilter";
import TeamList from "./components/TeamList";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import { ELO_SCORE, matchesSelectedElos, normalizeEloLabel, sortByElo } from "../../utils/elo";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? [];
}

function getInitials(name) {
    return String(name || "NKZ")
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((word) => word[0])
        .join("")
        .toUpperCase();
}

function normalizeTeamTag(tag, name) {
    const normalized = String(tag || "").trim().toUpperCase();
    if (normalized) return normalized;
    return getInitials(name).slice(0, 5) || "TIME";
}

function getAverageElo(players) {
    const rankedPlayers = players
        .map((player) => String(player.soloQueueTier || player.SoloQueueTier || "").toUpperCase())
        .filter((tier) => ELO_SCORE[tier]);

    if (!rankedPlayers.length) return "Unranked";

    const averageScore = rankedPlayers.reduce((total, tier) => total + ELO_SCORE[tier], 0) / rankedPlayers.length;
    const [closestTier] = Object.entries(ELO_SCORE).reduce((closest, current) => {
        return Math.abs(current[1] - averageScore) < Math.abs(closest[1] - averageScore) ? current : closest;
    });

    return normalizeEloLabel(closestTier);
}

function getTeamPoints(players, explicitPoints) {
    if (Number.isFinite(explicitPoints)) return explicitPoints;
    return players.reduce((total, player) => {
        const wins = player.wins ?? player.Wins ?? 0;
        const losses = player.losses ?? player.Losses ?? 0;
        return total + Math.max(0, wins * 3 - losses);
    }, 0);
}

function normalizePlayer(player, index) {
    return {
        id: player.id ?? player.Id ?? `${player.summonerName || "player"}-${index}`,
        userId: player.userId ?? player.UserId,
        teamId: player.teamId ?? player.TeamId,
        summonerName: player.summonerName ?? player.SummonerName ?? `Jogador ${index + 1}`,
        role: player.mainRole ?? player.MainRole ?? player.role ?? player.Role ?? player.position ?? player.Position ?? "Flex",
        elo: `${normalizeEloLabel(player.soloQueueTier ?? player.SoloQueueTier)} ${player.soloQueueRank ?? player.SoloQueueRank ?? ""}`.trim(),
        soloQueueTier: player.soloQueueTier ?? player.SoloQueueTier,
        isCaptain: player.isCaptain ?? player.IsCaptain ?? false,
        profileImageUrl: player.profileImageUrl ?? player.ProfileImageUrl ?? "",
        wins: player.wins ?? player.Wins ?? 0,
        losses: player.losses ?? player.Losses ?? 0,
    };
}

function getTournamentTeamIds(tournaments) {
    return new Set(
        (Array.isArray(tournaments) ? tournaments : [])
            .flatMap((tournament) => tournament.teams ?? tournament.Teams ?? [])
            .map((team) => team.id ?? team.Id)
            .filter(Boolean)
    );
}

function getTeamStatus(players, teamId, tournamentTeamIds = new Set()) {
    if (tournamentTeamIds.has(teamId)) {
        return { key: "in-tournament", label: "Em campeonato" };
    }

    if (players.length >= 5) {
        return { key: "full", label: "Completo" };
    }

    return { key: "recruiting", label: "Recrutando" };
}

function normalizeTeam(team, tournamentTeamIds = new Set()) {
    const players = (team.players ?? team.Players ?? []).map(normalizePlayer);
    const name = team.name ?? team.Name ?? "Time sem nome";
    const id = team.id ?? team.Id;
    const averageElo = team.averageElo ?? team.AverageElo ?? getAverageElo(players);
    const tag = normalizeTeamTag(team.tag ?? team.Tag, name);

    return {
        id,
        name,
        initials: getInitials(name),
        tag,
        players,
        playerCount: players.length,
        status: getTeamStatus(players, id, tournamentTeamIds),
        averageElo,
        points: getTeamPoints(players, team.points ?? team.Points),
        ownerId: team.ownerId ?? team.OwnerId,
        profileImageUrl: team.profileImageUrl ?? team.ProfileImageUrl ?? "",
    };
}

function normalizeInvitation(invitation, player) {
    return {
        id: invitation.id ?? invitation.Id,
        teamId: invitation.teamId ?? invitation.TeamId,
        playerId: invitation.playerId ?? invitation.PlayerId,
        type: invitation.type ?? invitation.Type ?? "Request",
        status: invitation.status ?? invitation.Status ?? "Pending",
        playerName: player?.summonerName ?? player?.SummonerName ?? "Jogador",
        playerRole: player?.role ?? player?.Role ?? "Flex",
    };
}

export default function TeamsPage() {
    const [teams, setTeams] = useState([]);
    const [selectedTeam, setSelectedTeam] = useState(null);
    const [createOpen, setCreateOpen] = useState(false);
    const [teamName, setTeamName] = useState("");
    const [teamTag, setTeamTag] = useState("");
    const [teamImage, setTeamImage] = useState(null);
    const [search, setSearch] = useState("");
    const [selectedElos, setSelectedElos] = useState([]);
    const [eloSort, setEloSort] = useState("none");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [requestLoading, setRequestLoading] = useState(false);
    const [requestFeedback, setRequestFeedback] = useState({ type: "", message: "" });
    const [teamInvitations, setTeamInvitations] = useState([]);
    const [pendingRequestTeamIds, setPendingRequestTeamIds] = useState([]);
    const [createLoading, setCreateLoading] = useState(false);
    const [createFeedback, setCreateFeedback] = useState({ type: "", message: "" });
    const [currentPlayer, setCurrentPlayer] = useState(null);
    const currentUser = getCurrentUser();

    async function loadTeams(isMounted = () => true) {
        try {
            setLoading(true);
            setError("");
            const [teamsResponse, tournamentsResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/team/ListTeams`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/Tournament`, { headers: getAuthHeaders() }).catch(() => ({ data: [] })),
            ]);

            if (!isMounted()) return;
            const teamIdsInTournament = getTournamentTeamIds(unwrapApiData(tournamentsResponse.data));
            const data = unwrapApiData(teamsResponse.data);
            setTeams((Array.isArray(data) ? data : []).map((team) => normalizeTeam(team, teamIdsInTournament)));
        } catch (requestError) {
            if (!isMounted()) return;
            setError(requestError?.response?.data?.message || "Erro ao carregar times.");
        } finally {
            if (isMounted()) setLoading(false);
        }
    }

    useEffect(() => {
        let isMounted = true;

        loadTeams(() => isMounted);

        if (currentUser?.userId) {
            axios.get(`${API_BASE_URL}/api/player/user/${currentUser.userId}`, { headers: getAuthHeaders() })
                .then(async (response) => {
                    const player = normalizePlayer(unwrapApiData(response.data), 0);
                    if (isMounted) setCurrentPlayer(player);

                    try {
                        const invitationsResponse = await axios.get(`${API_BASE_URL}/api/team/players/${player.id}/invitations`, {
                            headers: getAuthHeaders(),
                        });
                        const pendingTeamIds = (unwrapApiData(invitationsResponse.data) || [])
                            .filter((invitation) =>
                                String(invitation.type ?? invitation.Type).toLowerCase() === "request" &&
                                String(invitation.status ?? invitation.Status).toLowerCase() === "pending"
                            )
                            .map((invitation) => invitation.teamId ?? invitation.TeamId)
                            .filter(Boolean);
                        if (isMounted) setPendingRequestTeamIds(pendingTeamIds);
                    } catch {
                        if (isMounted) setPendingRequestTeamIds([]);
                    }
                })
                .catch(() => {
                    if (isMounted) setCurrentPlayer(null);
                });
        }

        return () => {
            isMounted = false;
        };
    }, [currentUser?.userId]);

    const filteredTeams = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();

        const filtered = teams.filter((team) => {
            const matchesName = team.name.toLowerCase().includes(normalizedSearch);
            const matchesElo = matchesSelectedElos(team.averageElo, selectedElos);
            return matchesName && matchesElo;
        });
        return sortByElo(filtered, eloSort, (team) => team.averageElo);
    }, [eloSort, search, selectedElos, teams]);

    function toggleElo(elo) {
        setSelectedElos((current) =>
            current.includes(elo) ? current.filter((item) => item !== elo) : [...current, elo]
        );
    }

    async function handleSelectTeam(team) {
        setRequestFeedback({ type: "", message: "" });
        setTeamInvitations([]);

        try {
            const [teamResponse, tournamentsResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/team/${team.id}`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/Tournament`, { headers: getAuthHeaders() }).catch(() => ({ data: [] })),
            ]);
            const teamIdsInTournament = getTournamentTeamIds(unwrapApiData(tournamentsResponse.data));
            const updated = normalizeTeam(unwrapApiData(teamResponse.data), teamIdsInTournament);
            setSelectedTeam(updated);
            await loadTeamInvitations(updated);
        } catch {
            setSelectedTeam(team);
            await loadTeamInvitations(team);
        }
    }

    async function refreshSelectedTeam(teamId = selectedTeam?.id) {
        if (!teamId) return;
        try {
            const [teamResponse, tournamentsResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/team/${teamId}`, { headers: getAuthHeaders() }),
                axios.get(`${API_BASE_URL}/api/Tournament`, { headers: getAuthHeaders() }).catch(() => ({ data: [] })),
            ]);
            const updated = normalizeTeam(unwrapApiData(teamResponse.data), getTournamentTeamIds(unwrapApiData(tournamentsResponse.data)));
            setSelectedTeam(updated);
            setTeams((current) => current.map((team) => team.id === updated.id ? updated : team));
            await loadTeamInvitations(updated);
        } catch {
            await loadTeams();
        }
    }

    async function loadTeamInvitations(team) {
        if (!team?.id || !currentUser) return;

        const currentPlayerId = currentPlayer?.id ?? currentPlayer?.Id;
        const currentUserId = currentUser?.userId;
        const isOwner = team.ownerId === currentUserId;
        const isAdmin = currentUser?.role === "Admin";
        const isCaptain = team.players.some((player) => (
            (player.id === currentPlayerId || player.userId === currentUserId) && player.isCaptain
        ));

        if (!isOwner && !isAdmin && !isCaptain) return;

        try {
            const response = await axios.get(`${API_BASE_URL}/api/team/${team.id}/invitations`, {
                headers: getAuthHeaders(),
            });
            const invitations = (unwrapApiData(response.data) || [])
                .filter((invitation) => String(invitation.status ?? invitation.Status).toLowerCase() === "pending");

            const enriched = await Promise.all(invitations.map(async (invitation) => {
                const playerId = invitation.playerId ?? invitation.PlayerId;
                try {
                    const playerResponse = await axios.get(`${API_BASE_URL}/api/player/${playerId}`, {
                        headers: getAuthHeaders(),
                    });
                    return normalizeInvitation(invitation, unwrapApiData(playerResponse.data));
                } catch {
                    return normalizeInvitation(invitation);
                }
            }));
            setTeamInvitations(enriched);
        } catch {
            setTeamInvitations([]);
        }
    }

    async function handleRequestJoin() {
        const currentUser = getCurrentUser();
        if (!currentUser?.userId) {
            setRequestFeedback({ type: "error", message: "Entre na sua conta para solicitar entrada." });
            return;
        }

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });

            const playerResponse = await axios.get(`${API_BASE_URL}/api/player/user/${currentUser.userId}`, {
                headers: getAuthHeaders(),
            });
            const player = unwrapApiData(playerResponse.data);
            const playerId = player?.id ?? player?.Id;

            if (!playerId) {
                setRequestFeedback({ type: "error", message: "Vincule seu jogador antes de solicitar entrada." });
                return;
            }

            if (selectedTeam.ownerId === currentUser.userId) {
                setRequestFeedback({ type: "error", message: "Voce ja e dono deste time." });
                return;
            }

            const invitation = {
                teamId: selectedTeam.id,
                playerId,
                senderId: currentUser.userId,
                type: "Request",
                status: "Pending",
            };

            const response = await axios.post(`${API_BASE_URL}/api/team/${selectedTeam.id}/invitations`, invitation, {
                headers: getAuthHeaders(),
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            if (success) {
                setPendingRequestTeamIds((current) => current.includes(selectedTeam.id) ? current : [...current, selectedTeam.id]);
            }
            setRequestFeedback({
                type: success ? "success" : "error",
                message: response.data?.message || response.data?.Message || (success ? "Solicitacao enviada." : "Nao foi possivel enviar a solicitacao."),
            });
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel enviar a solicitacao.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleLeaveTeam() {
        if (!currentPlayer?.id && !currentPlayer?.Id) {
            setRequestFeedback({ type: "error", message: "Vincule seu jogador antes de sair do time." });
            return;
        }

        if (!window.confirm("Tem certeza que deseja sair deste time?")) return;

        const playerId = currentPlayer.id ?? currentPlayer.Id;
        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/team/${selectedTeam.id}/players/${playerId}`, {
                headers: getAuthHeaders(),
            });
            setRequestFeedback({ type: "success", message: "Voce saiu do time." });
            await refreshSelectedTeam();
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel sair do time.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleDeleteTeam() {
        if (!selectedTeam?.id) return;
        if (!window.confirm("Tem certeza que deseja excluir este time? Essa acao nao pode ser desfeita.")) return;

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });
            await axios.delete(`${API_BASE_URL}/api/team/${selectedTeam.id}`, {
                headers: getAuthHeaders(),
            });
            setTeams((current) => current.filter((team) => team.id !== selectedTeam.id));
            setSelectedTeam(null);
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel excluir o time.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleExpelPlayer(player) {
        if (!window.confirm(`Tem certeza que deseja expulsar ${player.summonerName}?`)) return;

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/team/${selectedTeam.id}/players/${player.id}/expel`, null, {
                headers: getAuthHeaders(),
            });
            setRequestFeedback({ type: "success", message: "Jogador removido do time." });
            await refreshSelectedTeam();
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel expulsar o jogador.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleToggleCaptain(player, nextValue) {
        const action = nextValue ? "promover" : "remover o cargo de capitao de";
        if (!window.confirm(`Tem certeza que deseja ${action} ${player.summonerName}?`)) return;

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/team/${selectedTeam.id}/players/${player.id}/IsCaptain/${nextValue}`, null, {
                headers: getAuthHeaders(),
            });
            setRequestFeedback({ type: "success", message: nextValue ? "Jogador promovido a capitao." : "Capitao removido." });
            await refreshSelectedTeam();
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel atualizar o capitao.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleRespondInvitation(invitation, accept) {
        const action = accept ? "aceitar" : "recusar";
        if (!window.confirm(`Tem certeza que deseja ${action} a solicitacao de ${invitation.playerName}?`)) return;

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });
            await axios.post(`${API_BASE_URL}/api/team/invitations/${invitation.id}/respond`, { accept }, {
                headers: {
                    ...getAuthHeaders(),
                    "Content-Type": "application/json",
                },
            });
            setRequestFeedback({ type: "success", message: accept ? "Solicitacao aceita." : "Solicitacao recusada." });
            await refreshSelectedTeam();
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel responder a solicitacao.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function uploadTeamImage(teamId, image) {
        if (!image) return;
        const formData = new FormData();
        formData.append("image", image);
        const response = await axios.post(`${API_BASE_URL}/api/team/${teamId}/image`, formData, {
            headers: getAuthHeaders(),
        });
        return unwrapApiData(response.data);
    }

    async function handleUpdateTeam({ name, tag, image }) {
        if (!selectedTeam) return;

        try {
            setRequestLoading(true);
            setRequestFeedback({ type: "", message: "" });

            const nextTag = tag.trim().toUpperCase();
            if (!/^[A-Z]{3,5}$/.test(nextTag)) {
                setRequestFeedback({ type: "error", message: "A tag precisa ter de 3 a 5 letras." });
                return;
            }

            if ((name.trim() && name.trim() !== selectedTeam.name) || nextTag !== selectedTeam.tag) {
                await axios.put(`${API_BASE_URL}/api/team/${selectedTeam.id}`, {
                    id: selectedTeam.id,
                    name: name.trim(),
                    tag: nextTag,
                    ownerId: selectedTeam.ownerId,
                }, {
                    headers: getAuthHeaders(),
                });
            }

            await uploadTeamImage(selectedTeam.id, image);
            setRequestFeedback({ type: "success", message: "Time atualizado." });
            await refreshSelectedTeam();
        } catch (requestError) {
            setRequestFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel atualizar o time.",
            });
        } finally {
            setRequestLoading(false);
        }
    }

    async function handleCreateTeam(event) {
        event.preventDefault();
        const user = getCurrentUser();

        if (!user?.userId) {
            setCreateFeedback({ type: "error", message: "Entre na sua conta para criar um time." });
            return;
        }

        if (!teamName.trim()) {
            setCreateFeedback({ type: "error", message: "Informe o nome do time." });
            return;
        }

        const normalizedTag = teamTag.trim().toUpperCase();
        if (!/^[A-Z]{3,5}$/.test(normalizedTag)) {
            setCreateFeedback({ type: "error", message: "A tag precisa ter de 3 a 5 letras." });
            return;
        }

        try {
            setCreateLoading(true);
            setCreateFeedback({ type: "", message: "" });

            const response = await axios.post(`${API_BASE_URL}/api/team/${user.userId}?PlayerId=${user.userId}`, {
                name: teamName.trim(),
                tag: normalizedTag,
            }, {
                headers: getAuthHeaders(),
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            if (!success) {
                setCreateFeedback({
                    type: "error",
                    message: response.data?.message || response.data?.Message || "Nao foi possivel criar o time.",
                });
                return;
            }

            const newTeamId = response.data?.data ?? response.data?.Data;
            const uploadedTeam = teamImage && newTeamId ? await uploadTeamImage(newTeamId, teamImage) : null;
            const newTeam = normalizeTeam(uploadedTeam || {
                id: newTeamId,
                name: teamName.trim(),
                tag: normalizedTag,
                ownerId: user.userId,
                players: [],
            });

            setTeams((current) => [newTeam, ...current]);
            setTeamName("");
            setTeamTag("");
            setTeamImage(null);
            setCreateFeedback({ type: "success", message: "Time criado com sucesso." });
        } catch (requestError) {
            setCreateFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel criar o time.",
            });
        } finally {
            setCreateLoading(false);
        }
    }

    return (
        <main className="teams-page">
            <div className="teams-bg-grid" />
            <div className="teams-container">
                <div className="teams-heading">
                    <div>
                        <p className="teams-eyebrow">Times</p>
                        <h1>Equipes competitivas</h1>
                    </div>
                    <span>{filteredTeams.length} times encontrados</span>
                </div>

                {currentUser && (
                    <div className="teams-actions">
                        <button className="btn-primary" onClick={() => setCreateOpen(true)}>
                            Criar time
                        </button>
                    </div>
                )}

                <TeamFilter
                    search={search}
                    selectedElos={selectedElos}
                    eloSort={eloSort}
                    onSearchChange={setSearch}
                    onEloToggle={toggleElo}
                    onEloSortChange={setEloSort}
                />

                {loading && <section className="teams-state">Carregando times...</section>}
                {error && !loading && <section className="teams-state teams-state-error">{error}</section>}
                {!loading && !error && (
                    <TeamList
                        teams={filteredTeams}
                        selectedTeamId={selectedTeam?.id}
                        onSelect={handleSelectTeam}
                    />
                )}
            </div>

            <TeamDetails
                team={selectedTeam}
                loading={requestLoading}
                feedback={requestFeedback}
                currentUser={currentUser}
                currentPlayer={currentPlayer}
                onClose={() => setSelectedTeam(null)}
                onRequestJoin={handleRequestJoin}
                onLeaveTeam={handleLeaveTeam}
                onExpelPlayer={handleExpelPlayer}
                onToggleCaptain={handleToggleCaptain}
                onUpdateTeam={handleUpdateTeam}
                onDeleteTeam={handleDeleteTeam}
                invitations={teamInvitations}
                onRespondInvitation={handleRespondInvitation}
                hasPendingRequest={pendingRequestTeamIds.includes(selectedTeam?.id)}
            />

            <CreateTeamModal
                open={createOpen}
                teamName={teamName}
                teamTag={teamTag}
                loading={createLoading}
                feedback={createFeedback}
                onClose={() => setCreateOpen(false)}
                onSubmit={handleCreateTeam}
                onTeamNameChange={setTeamName}
                onTeamTagChange={setTeamTag}
                onTeamImageChange={setTeamImage}
            />
        </main>
    );
}
