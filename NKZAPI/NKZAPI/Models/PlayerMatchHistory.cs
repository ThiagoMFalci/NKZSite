using System.Text.Json.Serialization;

namespace NKZAPI.Models
{
    public class PlayerMatchHistory
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        [JsonIgnore]
        public Player? Player { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        public string ChampionName { get; set; } = "";
        public string Role { get; set; } = "Flex";
        public string QueueType { get; set; } = "Ranqueada";
        public bool Win { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
    }
}
