using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NKZAPI.Models;

namespace NKZAPI.Services.DiscordServices
{
    public class DiscordTeamRoleService : IDiscordTeamRoleService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DiscordTeamRoleService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task EnsureLeagueCategoryAsync()
        {
            using var request = CreateRequest(HttpMethod.Post, "/discord/bootstrap", "{}");
            if (request == null) return;
            await _httpClient.SendAsync(request);
        }

        public async Task SyncTeamRoleAsync(Team team)
        {
            if (team.Id == Guid.Empty) return;

            var players = (team.Players ?? Array.Empty<Player>())
                .Where(player => !string.IsNullOrWhiteSpace(player.DiscordUserId))
                .Select(player => new
                {
                    playerId = player.Id,
                    discordUserId = player.DiscordUserId,
                    discordUsername = player.DiscordUsername,
                    summonerName = player.SummonerName
                })
                .ToList();

            var payload = JsonSerializer.Serialize(new
            {
                teamId = team.Id,
                teamName = team.Name,
                teamTag = team.Tag,
                players
            });

            using var request = CreateRequest(HttpMethod.Post, "/teams/sync-role", payload);
            if (request == null) return;
            await _httpClient.SendAsync(request);
        }

        private HttpRequestMessage? CreateRequest(HttpMethod method, string path, string payload)
        {
            var baseUrl = _configuration["DiscordBot:BaseUrl"];
            var secret = _configuration["DiscordBot:Secret"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret)) return null;

            var request = new HttpRequestMessage(method, $"{baseUrl.TrimEnd('/')}{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            return request;
        }
    }
}
