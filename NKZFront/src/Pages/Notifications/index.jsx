import { useCallback, useEffect, useMemo, useState } from "react";
import axios from "axios";
import NotificationSection from "./components/NotificationSection";
import { getAuthHeaders, getCurrentUser } from "../../utils/auth";
import { getPlayerImageUrl } from "../../utils/images";
import "./style.css";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function unwrapApiData(responseData) {
    return responseData?.data ?? responseData?.Data ?? responseData ?? [];
}

function formatDate(value) {
    if (!value) return "Data indisponivel";
    return new Date(value).toLocaleDateString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    });
}

function formatDateTime(value) {
    if (!value) return "Horario ainda nao definido";
    return new Date(value).toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}

function normalizePlayer(player) {
    if (!player) return null;

    return {
        id: player.id ?? player.Id,
        userId: player.userId ?? player.UserId,
        name: player.summonerName ?? player.SummonerName ?? "Jogador",
        profileImageUrl: getPlayerImageUrl(player),
        role: player.role ?? player.Role ?? player.position ?? player.Position ?? "Flex",
        elo: `${player.soloQueueTier ?? player.SoloQueueTier ?? "Unranked"} ${player.soloQueueRank ?? player.SoloQueueRank ?? ""}`.trim(),
        isCaptain: player.isCaptain ?? player.IsCaptain ?? false,
    };
}

function normalizeTeam(team) {
    if (!team) return null;

    return {
        id: team.id ?? team.Id,
        name: team.name ?? team.Name ?? "Time",
        ownerId: team.ownerId ?? team.OwnerId,
        players: (team.players ?? team.Players ?? []).map(normalizePlayer).filter(Boolean),
    };
}

function normalizeInvitation(invitation) {
    return {
        id: invitation.id ?? invitation.Id,
        teamId: invitation.teamId ?? invitation.TeamId,
        playerId: invitation.playerId ?? invitation.PlayerId,
        senderId: invitation.senderId ?? invitation.SenderId,
        type: invitation.type ?? invitation.Type ?? "Invite",
        status: invitation.status ?? invitation.Status ?? "Pending",
        createdAt: invitation.createdAt ?? invitation.CreatedAt,
    };
}

export default function NotificationsPage() {
    const [player, setPlayer] = useState(null);
    const [teams, setTeams] = useState([]);
    const [allPlayers, setAllPlayers] = useState([]);
    const [leagues, setLeagues] = useState([]);
    const [playerInvitations, setPlayerInvitations] = useState([]);
    const [teamInvitations, setTeamInvitations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [feedback, setFeedback] = useState({ type: "", message: "" });
    const [responding, setResponding] = useState(false);
    const currentUser = getCurrentUser();

    const loadNotifications = useCallback(async () => {
        if (!currentUser?.userId) {
            setLoading(false);
            setError("Entre na sua conta para ver notificacoes.");
            return;
        }

        try {
            setLoading(true);
            setError("");
            setFeedback({ type: "", message: "" });

            const [teamsResponse, playersResponse, leaguesResponse] = await Promise.all([
                axios.get(`${API_BASE_URL}/api/team/ListTeams`, {
                    headers: getAuthHeaders(),
                }),
                axios.get(`${API_BASE_URL}/api/player`, {
                    headers: getAuthHeaders(),
                }).catch(() => ({ data: [] })),
                axios.get(`${API_BASE_URL}/api/league/ListLeagues`, {
                    headers: getAuthHeaders(),
                }).catch(() => ({ data: [] })),
            ]);
            const normalizedTeams = (unwrapApiData(teamsResponse.data) || []).map(normalizeTeam).filter(Boolean);
            setTeams(normalizedTeams);
            setAllPlayers((unwrapApiData(playersResponse.data) || []).map(normalizePlayer).filter(Boolean));
            setLeagues(unwrapApiData(leaguesResponse.data) || []);

            let currentPlayer = null;
            try {
                const playerResponse = await axios.get(`${API_BASE_URL}/api/player/user/${currentUser.userId}`, {
                    headers: getAuthHeaders(),
                });
                currentPlayer = normalizePlayer(unwrapApiData(playerResponse.data));
                setPlayer(currentPlayer);
            } catch {
                setPlayer(null);
            }

            if (currentPlayer?.id) {
                const invitationsResponse = await axios.get(`${API_BASE_URL}/api/team/players/${currentPlayer.id}/invitations`, {
                    headers: getAuthHeaders(),
                });
                setPlayerInvitations((unwrapApiData(invitationsResponse.data) || []).map(normalizeInvitation));
            } else {
                setPlayerInvitations([]);
            }

            const ownedTeams = normalizedTeams.filter((team) => (
                team.ownerId === currentUser.userId ||
                team.players.some((teamPlayer) => teamPlayer.userId === currentUser.userId && teamPlayer.isCaptain)
            ));
            const ownedInvitationLists = await Promise.all(
                ownedTeams.map(async (team) => {
                    try {
                        const response = await axios.get(`${API_BASE_URL}/api/team/${team.id}/invitations`, {
                            headers: getAuthHeaders(),
                        });
                        return unwrapApiData(response.data) || [];
                    } catch {
                        return [];
                    }
                })
            );
            setTeamInvitations(ownedInvitationLists.flat().map(normalizeInvitation));
        } catch (requestError) {
            setError(requestError?.response?.data?.message || "Erro ao carregar notificacoes.");
        } finally {
            setLoading(false);
        }
    }, [currentUser?.userId]);

    useEffect(() => {
        loadNotifications();
    }, [loadNotifications]);

    const teamMap = useMemo(() => {
        return teams.reduce((map, team) => {
            map[team.id] = team;
            return map;
        }, {});
    }, [teams]);

    const playerMap = useMemo(() => {
        return [
            ...allPlayers,
            ...teams.flatMap((team) => team.players),
        ]
            .reduce((map, teamPlayer) => {
                map[teamPlayer.id] = teamPlayer;
                return map;
            }, player ? { [player.id]: player } : {});
    }, [allPlayers, player, teams]);

    const groupedNotifications = useMemo(() => {
        const receivedInvites = playerInvitations
            .filter((invite) => invite.type === "Invite")
            .map((invite) => {
                const team = teamMap[invite.teamId];
                const sender = playerMap[invite.senderId];

                return {
                    ...invite,
                    typeLabel: "Convite recebido",
                    title: team?.name || "Time",
                    avatarUrl: sender?.profileImageUrl,
                    avatarLabel: sender?.name || "Capitao",
                    details: [
                        `Enviado por ${sender?.name || "Capitao do time"}`,
                    ],
                    dateLabel: formatDate(invite.createdAt),
                    canRespond: true,
                };
            });

        const sentRequests = playerInvitations
            .filter((invite) => invite.type === "Request")
            .map((invite) => {
                const team = teamMap[invite.teamId];

                return {
                    ...invite,
                    typeLabel: "Pedido enviado",
                    title: team?.name || "Time",
                    avatarUrl: player?.profileImageUrl,
                    avatarLabel: player?.name || "Jogador",
                    details: ["Solicitacao para entrar na equipe"],
                    dateLabel: formatDate(invite.createdAt),
                    canRespond: false,
                };
            });

        const receivedRequests = teamInvitations
            .filter((invite) => invite.type === "Request")
            .map((invite) => {
                const team = teamMap[invite.teamId];
                const requestedPlayer = playerMap[invite.playerId];

                return {
                    ...invite,
                    typeLabel: "Pedido recebido",
                    title: requestedPlayer?.name || "Jogador",
                    avatarUrl: requestedPlayer?.profileImageUrl,
                    avatarLabel: requestedPlayer?.name || "Jogador",
                    details: [
                        team?.name || "Time",
                        `Rota ${requestedPlayer?.role || "Flex"}`,
                        requestedPlayer?.elo || "Unranked",
                    ],
                    dateLabel: formatDate(invite.createdAt),
                    canRespond: true,
                };
            });

        const manageableTeamIds = new Set(teams
            .filter((team) => (
                team.ownerId === currentUser?.userId ||
                team.players.some((teamPlayer) => teamPlayer.userId === currentUser?.userId && teamPlayer.isCaptain)
            ))
            .map((team) => team.id));

        const scheduleNotifications = leagues.flatMap((league) => {
            const leagueName = league.name ?? league.Name ?? "Liga";
            const matches = league.matches ?? league.Matches ?? [];
            return matches.map((match) => {
                const teamAId = match.teamAId ?? match.TeamAId;
                const teamBId = match.teamBId ?? match.TeamBId;
                const status = match.status ?? match.Status ?? "Pending";
                const scheduleStatus = match.scheduleStatus ?? match.ScheduleStatus ?? "Open";
                const proposedByTeamId = match.proposedByTeamId ?? match.ProposedByTeamId;
                const proposedAt = match.proposedScheduledAt ?? match.ProposedScheduledAt ?? match.scheduledAt ?? match.ScheduledAt;
                const teamA = teamMap[teamAId];
                const teamB = teamMap[teamBId];
                const opponentId = manageableTeamIds.has(teamAId) ? teamBId : teamAId;
                const opponent = teamMap[opponentId];
                const proposedByMine = manageableTeamIds.has(proposedByTeamId);

                return {
                    id: match.id ?? match.Id,
                    teamAId,
                    teamBId,
                    status,
                    scheduleStatus,
                    typeLabel: "Agendamento",
                    title: `${teamA?.name || "Time A"} vs ${teamB?.name || "Time B"}`,
                    avatarLabel: opponent?.name || leagueName,
                    details: [
                        leagueName,
                        match.roundName ?? match.RoundName ?? `Semana ${match.weekNumber ?? match.WeekNumber ?? "-"}`,
                        proposedAt ? `Horario: ${formatDateTime(proposedAt)}` : "Sem horario sugerido",
                        scheduleStatus === "Pending"
                            ? (proposedByMine ? "Aguardando resposta do adversario" : "Outro time sugeriu um horario")
                            : scheduleStatus === "Confirmed"
                                ? "Horario confirmado"
                                : scheduleStatus === "Rejected"
                                    ? "Horario recusado, envie uma nova sugestao"
                                    : "Pendente de sugestao",
                    ],
                    dateLabel: proposedAt ? formatDateTime(proposedAt) : "Sem horario",
                    canRespond: false,
                };
            });
        }).filter((notification) => (
            notification.id &&
            notification.status !== "Completed" &&
            (manageableTeamIds.has(notification.teamAId) || manageableTeamIds.has(notification.teamBId))
        ));

        return { receivedInvites, sentRequests, receivedRequests, scheduleNotifications };
    }, [currentUser?.userId, leagues, player, playerInvitations, playerMap, teamInvitations, teamMap, teams]);

    async function handleRespond(invitationId, accept) {
        try {
            setResponding(true);
            setFeedback({ type: "", message: "" });

            const response = await axios.post(`${API_BASE_URL}/api/team/invitations/${invitationId}/respond`, { accept }, {
                headers: {
                    ...getAuthHeaders(),
                    "Content-Type": "application/json",
                },
            });

            const success = response.data?.success ?? response.data?.Success ?? true;
            setFeedback({
                type: success ? "success" : "error",
                message: response.data?.message || response.data?.Message || (success ? "Resposta enviada." : "Nao foi possivel responder."),
            });

            await loadNotifications();
        } catch (requestError) {
            setFeedback({
                type: "error",
                message: requestError?.response?.data?.message || "Nao foi possivel responder a notificacao.",
            });
        } finally {
            setResponding(false);
        }
    }

    return (
        <main className="notifications-page">
            <div className="notifications-bg-grid" />
            <div className="notifications-container">
                <div className="notifications-heading">
                    <div>
                        <p className="notifications-eyebrow">Central</p>
                        <h1>Notificacoes</h1>
                    </div>
                    <span>Times e convites</span>
                </div>

                {loading && <section className="notifications-state">Carregando notificacoes...</section>}
                {error && !loading && <section className="notifications-state notifications-state-error">{error}</section>}
                {feedback.message && <section className={`notifications-feedback ${feedback.type}`}>{feedback.message}</section>}

                {!loading && !error && (
                    <div className="notifications-grid">
                        <NotificationSection
                            title="Agendamentos"
                            description="Horarios de partidas das suas ligas: sugestoes recebidas, pendentes, recusadas e confirmadas."
                            notifications={groupedNotifications.scheduleNotifications}
                            onRespond={handleRespond}
                            responding={responding}
                        />
                        <NotificationSection
                            title="Convites recebidos"
                            description="Convites enviados por equipes para voce entrar no time."
                            notifications={groupedNotifications.receivedInvites}
                            onRespond={handleRespond}
                            responding={responding}
                        />
                        <NotificationSection
                            title="Pedidos enviados"
                            description="Solicitacoes que voce enviou para entrar em equipes."
                            notifications={groupedNotifications.sentRequests}
                            onRespond={handleRespond}
                            responding={responding}
                        />
                        <NotificationSection
                            title="Pedidos recebidos"
                            description="Jogadores que pediram entrada nos seus times."
                            notifications={groupedNotifications.receivedRequests}
                            onRespond={handleRespond}
                            responding={responding}
                        />
                    </div>
                )}
            </div>
        </main>
    );
}
