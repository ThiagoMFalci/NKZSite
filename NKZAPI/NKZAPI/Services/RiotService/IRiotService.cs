using System.Threading.Tasks;
using NKZAPI.Dtos;

namespace NKZAPI.Services.RiotService
{
    public interface IRiotService
    {
        Task<SummonerDto?> GetSummonerByNameAsync(string region, string summonerName);
        Task<LeagueEntryDto?> GetSoloQueueEntryAsync(string region, string summonerId);
        Task<string> ValidateApiKeyAsync(string region = "br1");
    }
}