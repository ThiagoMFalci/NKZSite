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

export const RANK_POINTS = {
    IRON: {
        IV: 1,
        III: 2,
        II: 3,
        I: 4,
    },
    BRONZE: {
        IV: 5,
        III: 6,
        II: 7,
        I: 8,
    },
    SILVER: {
        IV: 10,
        III: 11,
        II: 12,
        I: 13,
    },
    GOLD: {
        IV: 20,
        III: 21,
        II: 22,
        I: 23,
    },
    PLATINUM: {
        IV: 30,
        III: 31,
        II: 32,
        I: 33,
    },
    EMERALD: {
        IV: 40,
        III: 41,
        II: 42,
        I: 43,
    },
    DIAMOND: {
        IV: 55,
        III: 60,
        II: 70,
        I: 80,
    },
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
    const normalizedTier = String(tier || "").split(" ")[0].toUpperCase();
    const normalizedRank = String(rank || "").toUpperCase();
    const leaguePoints = Math.max(0, Number(lp) || 0);

    if (normalizedTier === "MASTER") return leaguePoints >= 300 ? 150 : 100;
    if (normalizedTier === "GRANDMASTER") return 175;
    if (normalizedTier === "CHALLENGER") return 200;

    return RANK_POINTS[normalizedTier]?.[normalizedRank] || 0;
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
