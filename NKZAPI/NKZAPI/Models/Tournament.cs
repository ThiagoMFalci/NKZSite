using System;
using System.Collections.Generic;

namespace NKZAPI.Models
{
    public class Tournament
    {
        public Guid Id { get; set; }
        // Owner of the tournament (user who created it)
        public Guid? OwnerId { get; set; }
        public string Name { get; set; }
        public int MaxTeams { get; set; }
        public double Prize { get; set; }
        public double EntryFee { get; set; }
        public string MinimumElo { get; set; } = "UNRANKED";
        public string MaximumElo { get; set; } = "CHALLENGER";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Modality { get; set; } = "Chaveamento";
        public List<Team> Teams { get; set; } = new List<Team>();
    }
}
