import "./style.css";

function normalizeTier(value) {
    const tier = String(value || "unranked").trim().split(" ")[0].toLowerCase();
    return tier || "unranked";
}

const RANK_ICON_BY_TIER = {
    iron: 1,
    bronze: 2,
    silver: 3,
    gold: 4,
    platinum: 5,
    emerald: 6,
    diamond: 7,
    master: 8,
    grandmaster: 9,
    challenger: 10,
};

export default function RankEmblem({ tier, label, className = "" }) {
    const normalizedTier = normalizeTier(tier);
    const alt = label || normalizedTier;
    const iconNumber = RANK_ICON_BY_TIER[normalizedTier] || 0;

    return (
        <span className={`rank-emblem-real ${className}`} title={alt}>
            <img
                src={`https://lolg-cdn.porofessor.gg/img/s/league-icons-v3/160/${iconNumber}.png`}
                alt={alt}
                loading="lazy"
                onError={(event) => {
                    event.currentTarget.src = "https://lolg-cdn.porofessor.gg/img/s/league-icons-v3/160/0.png";
                }}
            />
        </span>
    );
}
