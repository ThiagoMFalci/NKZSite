namespace NKZAPI.Models
{
    public class UserSubscription : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string Provider { get; set; } = "MercadoPago";
        public string? ProviderPreferenceId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? CheckoutUrl { get; set; }
        public DateTime StartsAt { get; set; } = DateTime.UtcNow;
        public DateTime EndsAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}
