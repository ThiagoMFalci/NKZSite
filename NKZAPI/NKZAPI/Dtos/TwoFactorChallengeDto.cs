namespace NKZAPI.Dtos
{
    public class TwoFactorChallengeDto
    {
        public bool RequiresTwoFactor { get; set; }
        public string TwoFactorToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
