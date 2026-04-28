import { BsBoxArrowRight, BsX } from "react-icons/bs";

export default function LeagueDetails({ league, loading, feedback, ownedTeam, onClose, onJoin, onLeave }) {
    if (!league) return null;

    const ownedTeamId = ownedTeam?.id ?? ownedTeam?.Id;
    const ownedTeamJoined = Boolean(ownedTeamId && league.teams.some((team) => team.id === ownedTeamId));

    return (
        <div className="league-modal-backdrop" onClick={onClose}>
            <section className="league-modal" onClick={(event) => event.stopPropagation()}>
                <button className="league-modal-close" onClick={onClose} aria-label="Fechar detalhes">
                    <BsX />
                </button>

                <div className="league-modal-header">
                    <div className="league-mark large">LG</div>
                    <div>
                        <p className="leagues-eyebrow">Detalhes da liga</p>
                        <h2>{league.name}</h2>
                        <span>{league.teamCount}/{league.maxTeams} times inscritos - Elo medio {league.averageElo}</span>
                    </div>
                </div>

                <div className="league-summary-grid">
                    <div><span>Premio</span><strong>{league.awardLabel}</strong></div>
                    <div><span>Entrada</span><strong>{league.entryFeeLabel}</strong></div>
                    <div><span>Status</span><strong>{league.status}</strong></div>
                </div>

                <div className="league-team-list">
                    {league.teams.length ? (
                        league.teams.map((team) => (
                            <article key={team.id || team.name} className="league-team-row">
                                <strong>{team.name}</strong>
                                <span>{team.playerCount} jogadores - {team.averageElo}</span>
                            </article>
                        ))
                    ) : (
                        <p className="leagues-empty">Ainda nao ha times inscritos nesta liga.</p>
                    )}
                </div>

                {feedback.message && (
                    <div className={`leagues-feedback ${feedback.type}`}>{feedback.message}</div>
                )}

                {ownedTeamJoined ? (
                    <button className="btn-primary league-join-button" onClick={onLeave} disabled={loading}>
                        <BsBoxArrowRight />
                        {loading ? "Saindo..." : "Sair da liga"}
                    </button>
                ) : (
                    <button className="btn-primary league-join-button" onClick={onJoin} disabled={loading || league.status === "Lotada"}>
                        {loading ? "Inscrevendo..." : "Inscrever meu time"}
                    </button>
                )}
            </section>
        </div>
    );
}
