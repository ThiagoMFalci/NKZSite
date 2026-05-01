namespace NKZAPI.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class CreatePlayerDto
    {
        [Required]
        [StringLength(32, MinimumLength = 3)]
        public string SummonerName { get; set; } = "";

        [StringLength(128)]
        public string RiotPuuid { get; set; } = "";

        [Range(0, 5000)]
        public int SummonerLevel { get; set; }

        [RegularExpression("^(Top|Jungle|Mid|ADC|Support|Flex)$")]
        public string MainRole { get; set; } = "Flex";

        public bool LookingForTeam { get; set; } = true;
    }
}
