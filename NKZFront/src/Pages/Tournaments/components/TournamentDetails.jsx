import { BsBoxArrowRight, BsPersonDashFill, BsTrash, BsX } from "react-icons/bs";

export default function TournamentDetails({
    tournament,
    loading,
    feedback,
    currentUser,
    ownedTeam,
    onClose,
    onJoin,
    onLeave,
    onDelete,
}) {
    if (!tournament) return null;

    const ownedTeamId = ownedTeam?.id ?? ownedTeam?.Id;
    const isOwner = tournament.ownerId === currentUser?.userId;
    const isAdmin = currentUser?.role === "Admin";
    const canManage = isOwner || isAdmin;
    const ownedTeamJoined = Boolean(ownedTeamId && tournament.teams.some((team) => team.id === ownedTeamId));

    return (
        <div className="tournament-modal-backdrop" onClick={onClose}>
            <section className="tournament-modal" onClick={(event) => event.stopPropagation()}>
                <button className="tournament-modal-close" onClick={onClose} aria-label="Fechar detalhes">
                    <BsX />
                </button>

                <div className="tournament-modal-header">
                    <div className="tournament-mark large">TR</div>
                    <div>
                        <p className="tournaments-eyebrow">Detalhes do campeonato</p>
                        <h2>{tournament.name}</h2>
                        <span>{tournament.teamCount}/{tournament.maxTeams} times inscritos - Elo medio {tournament.averageElo}</span>
                    </div>
                </div>

                <div className="tournament-summary-grid">
                    <div><span>Premio</span><strong>{tournament.prizeLabel}</strong></div>
                    <div><span>Entrada</span><strong>{tournament.entryFeeLabel}</strong></div>
                    <div><span>Status</span><strong>{tournament.status}</strong></div>
                </div>

                <div className="tournament-team-list">
                    {tournament.teams.length ? (
                        tournament.teams.map((team) => (
                            <article key={team.id || team.name} className="tournament-team-row">
                                <div>
                                    <strong>{team.name}</strong>
                                    <span> {team.playerCount} jogadores - {team.averageElo}</span>
                                </div>
                                {canManage && (
                                    <button
                                        className="inline-danger-button"
                                        type="button"
                                        onClick={() => onLeave(team.id)}
                                        disabled={loading}
                                    >
                                        <BsPersonDashFill />
                                        Expulsar
                                    </button>
                                )}
                            </article>
                        ))
                    ) : (
                        <p className="tournaments-empty">Ainda nao ha times inscritos neste campeonato.</p>
                    )}
                </div>

                {feedback.message && (
                    <div className={`tournaments-feedback ${feedback.type}`}>{feedback.message}</div>
                )}

                {canManage && (
                    <div className="tournament-danger-zone">
                        <div>
                            <strong>Excluir campeonato</strong>
                            <span>Remove o campeonato e libera os times inscritos.</span>
                        </div>
                        <button className="inline-danger-button" type="button" onClick={onDelete} disabled={loading}>
                            <BsTrash />
                            Excluir
                        </button>
                    </div>
                )}

                <div className="tournament-actions-footer">
                    {ownedTeamJoined ? (
                        <button className="btn-primary tournament-join-button" onClick={() => onLeave(ownedTeamId)} disabled={loading}>
                            <BsBoxArrowRight />
                            {loading ? "Saindo..." : "Sair do campeonato"}
                        </button>
                    ) : (
                        <button className="btn-primary tournament-join-button" onClick={onJoin} disabled={loading || tournament.status === "Lotado"}>
                            {loading ? "Inscrevendo..." : "Inscrever meu time"}
                        </button>
                    )}
                </div>
            </section>
        </div>
    );
}
