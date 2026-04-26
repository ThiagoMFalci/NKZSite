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

export function normalizeEloLabel(value) {
    const elo = String(value || "Unranked").trim();
    if (!elo) return "Unranked";
    return elo.charAt(0).toUpperCase() + elo.slice(1).toLowerCase();
}

export function getEloScore(value) {
    return ELO_SCORE[String(value || "").split(" ")[0].toUpperCase()] || 0;
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
