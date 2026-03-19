namespace NKZAPI.Models
{
    public class League : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<Team> Teams { get; set; } = new List<Team>();
        public float Award { get; set; }
        public float EntryFee { get; set; }
        public int MaxTeams { get; set; }


    }
}
