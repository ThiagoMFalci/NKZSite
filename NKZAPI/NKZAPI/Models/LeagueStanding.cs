namespace NKZAPI.Models
{
    public class LeagueStanding : BaseEntity
    {
        public Guid LeagueId { get; set; }
        public Guid TeamId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MapsPlayed { get; set; }
        public int MapDiff { get; set; }
        public int Penalties { get; set; }
        public int RatingPoints { get; set; } = 1500;
        public int LastRatingChange { get; set; }
    }
}
