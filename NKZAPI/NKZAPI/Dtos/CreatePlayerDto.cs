namespace NKZAPI.Dtos
{
    public class CreatePlayerDto
    {
        public string SummonerName { get; set; } = "";
        public string RiotPuuid { get; set; } = "";
        public int SummonerLevel { get; set; }
        public string MainRole { get; set; } = "Flex";
        public bool LookingForTeam { get; set; } = true;
    }
}
