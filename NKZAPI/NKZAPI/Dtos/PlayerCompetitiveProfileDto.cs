namespace NKZAPI.Dtos
{
    public class PlayerCompetitiveProfileDto
    {
        public string MainRole { get; set; } = "Flex";
        public bool LookingForTeam { get; set; } = true;
        public List<string> Tags { get; set; } = new();
    }
}
