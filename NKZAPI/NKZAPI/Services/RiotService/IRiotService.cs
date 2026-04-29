using System.Threading.Tasks;
using System.Text.Json;
using NKZAPI.Dtos;

namespace NKZAPI.Services.RiotService
{
    public interface IRiotService
    {
        Task<RiotAccountDto?> GetAccountByRiotIdAsync(string regionalRoute, string gameName, string tagLine);
        Task<SummonerDto?> GetSummonerByPuuidAsync(string region, string puuid);
        Task<SummonerDto?> GetSummonerByNameAsync(string region, string summonerName);
        Task<LeagueEntryDto?> GetSoloQueueEntryAsync(string region, string summonerId);
        Task<LeagueEntryDto?> GetSoloQueueEntryByPuuidAsync(string region, string puuid);
        Task<List<string>> GetRecentMatchIdsAsync(string regionalRoute, string puuid, int count = 5);
        Task<JsonDocument?> GetMatchAsync(string regionalRoute, string matchId);
        Task<string> ValidateApiKeyAsync(string region = "br1");
    }
}
