using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace NKZAPI.Models
{
    public class Player : BaseEntity
    {
        // Owner
        public Guid? UserId { get; set; }
        // Adicione esta propriedade para corrigir o erro CS1061

        public Guid? TeamId { get; set; }

        public bool IsCaptain { get; set; } = false;
        public string MainRole { get; set; } = "Flex";
        public bool LookingForTeam { get; set; } = true;
        public string Tags { get; set; } = "";

        // Persist the image URL/path
        public string? ProfileImageUrl { get; set; }
        [NotMapped]
        public string? DiscordUserId { get; set; }
        [NotMapped]
        public string? DiscordUsername { get; set; }

        // Temp upload field, not mapped to the database
        [NotMapped]
        public IFormFile? ProfileImageFile { get; set; }

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

        public List<PlayerChampionStat> ChampionStats { get; set; } = new();
        public List<PlayerRoleStat> RoleStats { get; set; } = new();
        public List<PlayerMatchHistory> MatchHistory { get; set; } = new();
    }
}
