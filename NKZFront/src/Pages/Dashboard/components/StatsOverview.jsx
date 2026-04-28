import { BsBullseye, BsGraphUpArrow, BsTrophyFill, BsXCircleFill } from "react-icons/bs";

const statIcons = {
    totalMatches: <BsBullseye />,
    wins: <BsTrophyFill />,
    losses: <BsXCircleFill />,
    winRate: <BsGraphUpArrow />,
};

export default function StatsOverview({ stats }) {
    const items = [
        { key: "totalMatches", label: "Partidas", value: stats.totalMatches },
        { key: "wins", label: "Vitorias", value: stats.wins },
        { key: "losses", label: "Derrotas", value: stats.losses },
        { key: "winRate", label: "Win rate", value: `${stats.winRate}%` },
    ];

    return (
        <section className="stats-overview">
            {items.map((item) => (
                <article key={item.key} className={`dashboard-card stat-card stat-${item.key}`}>
                    <div className="stat-card-icon">{statIcons[item.key]}</div>
                    <div>
                        <span>{item.label}</span>
                        <strong>{item.value}</strong>
                    </div>
                    {item.key === "winRate" && (
                        <div className="winrate-track">
                            <span style={{ width: `${Math.min(stats.winRate, 100)}%` }} />
                        </div>
                    )}
                </article>
            ))}
        </section>
    );
}
