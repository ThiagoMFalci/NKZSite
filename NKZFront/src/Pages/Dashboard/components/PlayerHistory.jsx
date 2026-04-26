import { BsActivity, BsClockHistory, BsGraphUpArrow } from "react-icons/bs";

export default function PlayerHistory({ recentMatches, champions, eloHistory }) {
    const topChampions = champions.slice(0, 3);

    return (
        <section className="dashboard-history-grid">
            <article className="dashboard-card dashboard-history-card">
                <div className="dashboard-card-heading">
                    <BsClockHistory />
                    <h2>Ultimas partidas</h2>
                </div>
                {recentMatches.length ? (
                    <div className="history-list">
                        {recentMatches.map((match) => (
                            <div key={match.id} className="history-row">
                                <strong>{match.champion}</strong>
                                <span>{match.mode}</span>
                                <em className={String(match.result).toLowerCase().includes("v") ? "win" : "loss"}>
                                    {match.result}
                                </em>
                                <small>{match.kda}</small>
                            </div>
                        ))}
                    </div>
                ) : (
                    <p className="dashboard-empty">As proximas partidas sincronizadas aparecem aqui.</p>
                )}
            </article>

            <article className="dashboard-card dashboard-history-card">
                <div className="dashboard-card-heading">
                    <BsActivity />
                    <h2>Campeoes mais usados</h2>
                </div>
                {topChampions.length ? (
                    <div className="history-list">
                        {topChampions.map((champion) => (
                            <div key={champion.name} className="history-row compact">
                                <strong>{champion.name}</strong>
                                <span>{champion.matches} partidas</span>
                                <em>{champion.winRate}% WR</em>
                            </div>
                        ))}
                    </div>
                ) : (
                    <p className="dashboard-empty">Sem campeoes registrados ainda.</p>
                )}
            </article>

            <article className="dashboard-card dashboard-history-card">
                <div className="dashboard-card-heading">
                    <BsGraphUpArrow />
                    <h2>Evolucao de elo</h2>
                </div>
                <div className="elo-history-track">
                    {eloHistory.map((entry, index) => (
                        <div key={`${entry.label}-${index}`} className="elo-history-step">
                            <span />
                            <strong>{entry.tier} {entry.division}</strong>
                            <small>{entry.label} - {entry.lp} LP</small>
                        </div>
                    ))}
                </div>
            </article>
        </section>
    );
}
