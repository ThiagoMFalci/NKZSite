using System.Text.Json.Serialization;

namespace NKZAPI.Dtos
{
    public class LeagueEntryDto
    {
        [JsonPropertyName("queueType")]
        public string QueueType { get; set; } = string.Empty;
        [JsonPropertyName("tier")]
        public string Tier { get; set; } = string.Empty;
        [JsonPropertyName("rank")]
        public string Rank { get; set; } = string.Empty;
        [JsonPropertyName("leaguePoints")]
        public int LeaguePoints { get; set; }
        [JsonPropertyName("wins")]
        public int Wins { get; set; }
        [JsonPropertyName("losses")]
        public int Losses { get; set; }
    }
}