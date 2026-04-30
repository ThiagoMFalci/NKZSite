namespace NKZAPI.Models
{
    public static class CompetitivePoints
    {
        public static int FromPlayer(Player player)
        {
            return FromRank(player.SoloQueueTier, player.SoloQueueRank, player.SoloQueueLP);
        }

        public static int FromRank(string? tier, string? rank, int lp)
        {
            var normalizedTier = (tier ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToUpperInvariant() ?? "";
            var normalizedRank = (rank ?? "").Trim().ToUpperInvariant();
            var leaguePoints = Math.Max(0, lp);

            if (normalizedTier == "MASTER") return leaguePoints >= 300 ? 150 : 100;

            return normalizedTier switch
            {
                "IRON" => normalizedRank switch
                {
                    "IV" => 1,
                    "III" => 2,
                    "II" => 3,
                    "I" => 4,
                    _ => 0
                },
                "BRONZE" => normalizedRank switch
                {
                    "IV" => 5,
                    "III" => 6,
                    "II" => 7,
                    "I" => 8,
                    _ => 0
                },
                "SILVER" => normalizedRank switch
                {
                    "IV" => 10,
                    "III" => 11,
                    "II" => 12,
                    "I" => 13,
                    _ => 0
                },
                "GOLD" => normalizedRank switch
                {
                    "IV" => 20,
                    "III" => 21,
                    "II" => 22,
                    "I" => 23,
                    _ => 0
                },
                "PLATINUM" => normalizedRank switch
                {
                    "IV" => 30,
                    "III" => 31,
                    "II" => 32,
                    "I" => 33,
                    _ => 0
                },
                "EMERALD" => normalizedRank switch
                {
                    "IV" => 40,
                    "III" => 41,
                    "II" => 42,
                    "I" => 43,
                    _ => 0
                },
                "DIAMOND" => normalizedRank switch
                {
                    "IV" => 55,
                    "III" => 60,
                    "II" => 70,
                    "I" => 80,
                    _ => 0
                },
                "GRANDMASTER" => 175,
                "CHALLENGER" => 200,
                _ => 0
            };
        }
    }
}
