namespace NKZAPI.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class LeagueMatchResultDto
    {
        public Guid WinnerTeamId { get; set; }

        [Range(0, 5)]
        public int TeamAScore { get; set; } = 1;

        [Range(0, 5)]
        public int TeamBScore { get; set; }
    }
}
