using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace NKZAPI.Models
{
    public class Player : BaseEntity
    {
        // Adicione esta propriedade para corrigir o erro CS1061

        public List<Profiler> Role { get; set; } = new List<Profiler>();

        public Guid? TeamId { get; set; }

        public bool IsCaptain { get; set; } = false;

        // Riot
        public string SummonerName { get; set; } = null!;
        public string RiotPuuid { get; set; } = null!;
        public int SummonerLevel { get; set; }

        public string SoloQueueTier { get; set; } = "UNRANKED";
        public string SoloQueueRank { get; set; } = "";
        public int SoloQueueLP { get; set; }

        // Stats agregadas
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        // Controle
        public DateTime LastStatsUpdate { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
