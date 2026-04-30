export const ELO_OPTIONS = [
    "Iron",
    "Bronze",
    "Silver",
    "Gold",
    "Platinum",
    "Emerald",
    "Diamond",
    "Master",
    "Grandmaster",
    "Challenger",
];

export const ELO_SCORE = {
    IRON: 1,
    BRONZE: 2,
    SILVER: 3,
    GOLD: 4,
    PLATINUM: 5,
    EMERALD: 6,
    DIAMOND: 7,
    MASTER: 8,
    GRANDMASTER: 9,
    CHALLENGER: 10,
};

export const RANK_SCORE = {
    IV: 1,
    III: 2,
    II: 3,
    I: 4,
};

export function normalizeEloLabel(value) {
    const elo = String(value || "Unranked").trim();
    if (!elo) return "Unranked";
    return elo.charAt(0).toUpperCase() + elo.slice(1).toLowerCase();
}

export function getEloScore(value) {
    return ELO_SCORE[String(value || "").split(" ")[0].toUpperCase()] || 0;
}

export function calculateWinRate(wins = 0, losses = 0) {
    const total = wins + losses;
    return total ? Math.round((wins / total) * 100) : 0;
}

export function calculateRankPoints(tier, rank, lp = 0) {
    const tierValue = getEloScore(tier);
    const rankValue = RANK_SCORE[String(rank || "").toUpperCase()] || 0;
    return tierValue * 1000 + rankValue * 100 + Math.max(0, Number(lp) || 0);
}

export function matchesSelectedElos(value, selectedElos = []) {
    if (!selectedElos.length) return true;
    const tier = String(value || "").split(" ")[0].toLowerCase();
    return selectedElos.some((elo) => elo.toLowerCase() === tier);
}

export function sortByElo(items, direction, getValue) {
    if (direction === "none") return items;

    return [...items].sort((a, b) => {
        const diff = getEloScore(getValue(a)) - getEloScore(getValue(b));
        return direction === "asc" ? diff : -diff;
    });
}
