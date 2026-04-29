using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace NKZAPI.Models
{
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Tag { get; set; }
        public Guid? OwnerId { get; set; }
        public bool IsRecruiting { get; set; } = true;
        public ICollection<Player>? Players { get; set; }
        // Persisted image path/URL
        public string? ProfileImageUrl { get; set; }

        // Temporary upload field, not mapped to DB
        [NotMapped]
        public IFormFile? ProfileImageFile { get; set; }
    }
}
