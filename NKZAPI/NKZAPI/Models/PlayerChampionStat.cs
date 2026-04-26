using System.Text.Json.Serialization;

namespace NKZAPI.Models
{
    public class PlayerChampionStat
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        [JsonIgnore]
        public Player? Player { get; set; }
        public string ChampionName { get; set; } = "";
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
    }
}
