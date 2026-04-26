import { useEffect, useState } from "react";
import { BsBoxArrowRight, BsCheck2, BsPersonDashFill, BsPersonPlusFill, BsShieldFillCheck, BsTrash, BsX } from "react-icons/bs";

const API_BASE_URL = import.meta.env.VITE_API_URL || "";

function resolveImageUrl(url) {
    if (!url) return "";
    if (/^https?:\/\//i.test(url)) return url;
    return `${API_BASE_URL}/${url}`.replace(/([^:]\/)\/+/g, "$1");
}

export default function TeamDetails({
    team,
    loading,
    feedback,
    currentUser,
    currentPlayer,
    onClose,
    onRequestJoin,
    onLeaveTeam,
    onExpelPlayer,
    onToggleCaptain,
    onUpdateTeam,
    onDeleteTeam,
    invitations = [],
    onRespondInvitation,
    hasPendingRequest = false,
}) {
    const [imagePreview, setImagePreview] = useState("");

    useEffect(() => {
        return () => {
            if (imagePreview) URL.revokeObjectURL(imagePreview);
        };
    }, [imagePreview]);

    if (!team) return null;

    function closeDetails() {
        if (imagePreview) URL.revokeObjectURL(imagePreview);
        setImagePreview("");
        onClose();
    }

    const currentPlayerId = currentPlayer?.id ?? currentPlayer?.Id;
    const currentPlayerTeamId = currentPlayer?.teamId ?? currentPlayer?.TeamId;
    const currentUserId = currentUser?.userId;
    const isOwner = team.ownerId === currentUserId;
    const isAdmin = currentUser?.role === "Admin";
    const canManage = isOwner || isAdmin;
    const isCaptain = team.players.some((player) => (
        (player.id === currentPlayerId || player.userId === currentUserId) && player.isCaptain
    ));
    const canReviewRequests = canManage || isCaptain;
    const isMember = team.players.some((player) => player.id === currentPlayerId || player.userId === currentUserId);
    const isInOtherTeam = Boolean(currentPlayerTeamId && currentPlayerTeamId !== team.id && !isMember);
    const teamUnavailable = team.status?.key === "full" || team.status?.key === "in-tournament";
    const canRequestJoin = Boolean(currentUser && currentPlayerId && !isOwner && !isMember && !isInOtherTeam && !hasPendingRequest && !teamUnavailable);
    const previewSrc = imagePreview || resolveImageUrl(team.profileImageUrl);
    const roleKeys = ["Top", "Jungle", "Mid", "ADC", "Support"];
    const filledRoles = team.players.map((player) => String(player.role || "").toLowerCase());
    const missingRoles = roleKeys.filter((role) => !filledRoles.includes(role.toLowerCase()));
    const teamWins = team.players.reduce((total, player) => total + (player.wins || 0), 0);
    const teamLosses = team.players.reduce((total, player) => total + (player.losses || 0), 0);
    const teamWinRate = teamWins + teamLosses ? Math.round((teamWins / (teamWins + teamLosses)) * 100) : 0;
    const requestButtonLabel = (() => {
        if (!currentPlayerId) return "Vincule um jogador";
        if (hasPendingRequest) return "Solicitacao enviada";
        if (isInOtherTeam) return "Voce ja esta em outro time";
        if (team.status?.key === "full") return "Time completo";
        if (team.status?.key === "in-tournament") return "Time em campeonato";
        return loading ? "Enviando..." : "Solicitar entrada";
    })();

    return (
        <div className="team-modal-backdrop" onClick={closeDetails}>
            <section className="team-modal" onClick={(event) => event.stopPropagation()}>
                <button className="team-modal-close" onClick={closeDetails} aria-label="Fechar detalhes">
                    <BsX />
                </button>

                <div className="team-modal-header team-detail-header">
                    <div className="team-mark large">
                        <span className="team-mark-fallback">{team.initials}</span>
                        {team.profileImageUrl && (
                            <img
                                src={resolveImageUrl(team.profileImageUrl)}
                                alt={team.name}
                                onError={(event) => {
                                    event.currentTarget.style.display = "none";
                                }}
                            />
                        )}
                    </div>
                    <div className="team-detail-title">
                        <p className="teams-eyebrow">Detalhes do time</p>
                        <h2>{team.name}</h2>
                        <div className="team-detail-meta">
                            <span>{team.tag}</span>
                            <span>{team.playerCount}/5 jogadores</span>
                            <span>{team.averageElo}</span>
                            <span>{team.points} pts</span>
                        </div>
                        <em className={`team-status-badge ${team.status?.key || "recruiting"}`}>
                            {team.status?.label || "Recrutando"}
                        </em>
                    </div>
                </div>

                {canManage && (
                    <form
                        className="team-edit-form team-panel"
                        onSubmit={(event) => {
                            event.preventDefault();
                            const form = event.currentTarget;
                            onUpdateTeam({
                                name: form.elements.teamName.value,
                                tag: form.elements.teamTag.value,
                                image: form.elements.teamImage.files?.[0] || null,
                            });
                        }}
                    >
                        <div className="team-panel-heading">
                            <strong>Editar time</strong>
                            <span>Atualize identidade e imagem da equipe.</span>
                        </div>
                        <label className="team-form-field">
                            Nome do time
                            <input name="teamName" defaultValue={team.name} />
                        </label>
                        <label className="team-form-field">
                            Tag
                            <input
                                name="teamTag"
                                defaultValue={team.tag}
                                minLength={3}
                                maxLength={5}
                                onInput={(event) => {
                                    event.currentTarget.value = event.currentTarget.value.replace(/[^a-zA-Z]/g, "").toUpperCase();
                                }}
                            />
                        </label>
                        <label className="team-image-preview-picker">
                            <span>Imagem do time</span>
                            <div className="team-image-preview">
                                {previewSrc ? <img src={previewSrc} alt={team.name} /> : <strong>{team.initials}</strong>}
                            </div>
                            <strong>Escolher imagem</strong>
                            <input
                                name="teamImage"
                                type="file"
                                accept="image/*"
                                onChange={(event) => {
                                    const file = event.currentTarget.files?.[0];
                                    if (imagePreview) URL.revokeObjectURL(imagePreview);
                                    setImagePreview(file ? URL.createObjectURL(file) : "");
                                }}
                            />
                        </label>
                        <button className="team-action-button primary" type="submit" disabled={loading}>
                            Salvar alteracoes
                        </button>
                    </form>
                )}

                <div className="team-panel team-stats-panel">
                    <div className="team-panel-heading">
                        <strong>Resumo competitivo</strong>
                        <span>Media e lacunas do elenco atual.</span>
                    </div>
                    <div className="team-summary-grid">
                        <span><strong>{team.averageElo}</strong>Elo medio</span>
                        <span><strong>{teamWinRate}%</strong>Win rate medio</span>
                        <span><strong>{missingRoles.length ? missingRoles.join(", ") : "Completo"}</strong>Roles faltando</span>
                    </div>
                </div>

                <div className="players-list team-panel">
                    <div className="team-panel-heading">
                        <strong>Jogadores</strong>
                        <span>{team.playerCount}/5 vagas ocupadas</span>
                    </div>
                    {team.players.length ? (
                        team.players.map((player) => (
                            <article key={player.id || player.summonerName} className="player-row">
                                <div className="player-row-avatar">
                                    <span>{player.summonerName.slice(0, 2).toUpperCase()}</span>
                                    {player.profileImageUrl && (
                                        <img
                                            src={resolveImageUrl(player.profileImageUrl)}
                                            alt={player.summonerName}
                                            onError={(event) => {
                                                event.currentTarget.style.display = "none";
                                            }}
                                        />
                                    )}
                                </div>
                                <div className="player-main">
                                    <strong>{player.summonerName}</strong>
                                    <span>{player.role}{player.isCaptain ? " - Capitao" : ""}</span>
                                </div>
                                <span className="player-elo">{player.elo}</span>
                                {canManage && player.userId !== team.ownerId && (
                                    <div className="player-actions">
                                        <button type="button" onClick={() => onToggleCaptain(player, !player.isCaptain)} disabled={loading}>
                                            <BsShieldFillCheck />
                                            {player.isCaptain ? "Remover capitao" : "Promover capitao"}
                                        </button>
                                        <button type="button" onClick={() => onExpelPlayer(player)} disabled={loading}>
                                            <BsPersonDashFill />
                                            Expulsar
                                        </button>
                                    </div>
                                )}
                            </article>
                        ))
                    ) : (
                        <p className="teams-empty">Este time ainda nao possui jogadores listados.</p>
                    )}
                </div>

                {canReviewRequests && invitations.length > 0 && (
                    <div className="team-panel team-requests-panel">
                        <div className="team-panel-heading">
                            <strong>Solicitacoes</strong>
                            <span>Capitaes podem aceitar ou recusar pedidos de entrada.</span>
                        </div>
                        {invitations.map((invitation) => (
                            <article key={invitation.id} className="team-request-row">
                                <div>
                                    <strong>{invitation.playerName}</strong>
                                    <span>{invitation.playerRole} - aguardando resposta</span>
                                </div>
                                <div className="player-actions">
                                    <button type="button" onClick={() => onRespondInvitation(invitation, true)} disabled={loading}>
                                        <BsCheck2 />
                                        Aceitar
                                    </button>
                                    <button type="button" onClick={() => onRespondInvitation(invitation, false)} disabled={loading}>
                                        <BsX />
                                        Recusar
                                    </button>
                                </div>
                            </article>
                        ))}
                    </div>
                )}

                {feedback.message && (
                    <div className={`teams-feedback ${feedback.type}`}>{feedback.message}</div>
                )}

                {canManage && (
                    <div className="team-danger-zone team-panel">
                        <div className="team-panel-heading">
                            <strong>Excluir time</strong>
                            <span>Remove o time, jogadores vinculados e solicitacoes relacionadas.</span>
                        </div>
                        <button className="team-action-button danger" type="button" onClick={onDeleteTeam} disabled={loading}>
                            <BsTrash />
                            Excluir time
                        </button>
                    </div>
                )}

                <div className="team-actions-footer">
                    {isOwner ? (
                        <div className="team-owner-note">
                            <BsShieldFillCheck />
                            Voce e dono deste time
                        </div>
                    ) : isMember ? (
                        <button className="team-action-button danger team-request-button" onClick={onLeaveTeam} disabled={loading}>
                            <BsBoxArrowRight />
                            {loading ? "Saindo..." : "Sair do time"}
                        </button>
                    ) : (
                        <button className="team-action-button primary team-request-button" onClick={onRequestJoin} disabled={loading || !canRequestJoin}>
                            <BsPersonPlusFill />
                            {requestButtonLabel}
                        </button>
                    )}
                </div>
            </section>
        </div>
    );
}
