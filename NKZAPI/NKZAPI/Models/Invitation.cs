using System;

namespace NKZAPI.Models
{
    public class Invitation : BaseEntity
    {
        public Guid TeamId { get; set; }
        public Guid PlayerId { get; set; }
        public Guid SenderId { get; set; }
        public string Type { get; set; } = "Invite"; // "Invite" or "Request"
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Declined", "Cancelled"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}
