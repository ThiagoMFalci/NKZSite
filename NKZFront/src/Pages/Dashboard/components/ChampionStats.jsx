import { GiCrossedSwords } from "react-icons/gi";

export default function ChampionStats({ champions }) {
    if (!champions.length) {
        return (
            <section className="dashboard-card dashboard-list-card">
                <div className="dashboard-card-heading">
                    <p className="dashboard-eyebrow">Campeoes</p>
                    <h2>Mais jogados</h2>
                </div>
                <p className="dashboard-empty">Nenhuma estatistica de campeao encontrada.</p>
            </section>
        );
    }

    return (
        <section className="dashboard-card dashboard-list-card">
            <div className="dashboard-card-heading">
                <p className="dashboard-eyebrow">Campeoes</p>
                <h2>Mais jogados</h2>
            </div>

            <div className="champion-grid">
                {champions.map((champion, index) => (
                    <article key={champion.name} className={`champion-card ${index === 0 ? "most-played" : ""}`}>
                        <div className="champion-avatar">
                            {champion.imageUrl ? (
                                <img src={champion.imageUrl} alt={champion.name} />
                            ) : (
                                <GiCrossedSwords />
                            )}
                        </div>

                        <div className="champion-info">
                            <strong>{champion.name}</strong>
                            <span>{champion.matches} partidas</span>
                        </div>

                        <div className="champion-metrics">
                            <span>{champion.wins}V</span>
                            <span>{champion.losses}D</span>
                            <strong>{champion.winRate}%</strong>
                        </div>
                    </article>
                ))}
            </div>
        </section>
    );
}
