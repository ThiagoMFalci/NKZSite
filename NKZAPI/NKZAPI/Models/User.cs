namespace NKZAPI.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public List<Player> Player { get; set; } = null!;
    }
}
