using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Models
{
    public class SubscriptionPlan : BaseEntity
    {
        [Required]
        [StringLength(80, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 10000)]
        public decimal Price { get; set; }

        [Range(1, 36)]
        public int DurationMonths { get; set; } = 1;

        [StringLength(1000)]
        public string Benefits { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
