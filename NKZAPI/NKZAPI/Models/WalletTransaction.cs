namespace NKZAPI.Models
{
    public class WalletTransaction : BaseEntity
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Credit";
        public string Description { get; set; } = string.Empty;
        public Guid? LeagueId { get; set; }
        public Guid? TeamId { get; set; }
        public Guid? LeaguePaymentId { get; set; }
        public Guid? WalletPaymentId { get; set; }
    }
}
