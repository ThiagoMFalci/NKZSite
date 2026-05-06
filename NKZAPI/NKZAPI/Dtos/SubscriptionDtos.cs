using System.ComponentModel.DataAnnotations;

namespace NKZAPI.Dtos
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMonths { get; set; }
        public string Benefits { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateSubscriptionPlanDto
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
    }

    public class SubscriptionCheckoutDto
    {
        [Required]
        public Guid PlanId { get; set; }
    }

    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
