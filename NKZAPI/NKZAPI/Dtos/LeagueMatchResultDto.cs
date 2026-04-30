namespace NKZAPI.Dtos
{
    public class LeagueMatchResultDto
    {
        public Guid WinnerTeamId { get; set; }
        public int TeamAScore { get; set; } = 1;
        public int TeamBScore { get; set; }
    }
}
