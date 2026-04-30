namespace NKZAPI.Models
{
    public class WalletPayment : BaseEntity
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string Provider { get; set; } = "MercadoPago";
        public string? ProviderPreferenceId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? CheckoutUrl { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
    }
}
