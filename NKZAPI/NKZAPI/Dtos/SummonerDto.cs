using System.Text.Json.Serialization;

namespace NKZAPI.Dtos
{
    public class SummonerDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // summonerId
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;
        [JsonPropertyName("puuid")]
        public string Puuid { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("summonerLevel")]
        public long SummonerLevel { get; set; }
    }
}