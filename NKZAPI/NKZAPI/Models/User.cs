namespace NKZAPI.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public List<Player> Player { get; set; } = null!;
    }
}
