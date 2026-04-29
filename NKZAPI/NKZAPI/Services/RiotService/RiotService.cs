using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NKZAPI.Dtos;

namespace NKZAPI.Services.RiotService
{
    public class RiotService : IRiotService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _apiKey;
        private readonly ILogger<RiotService> _logger;

        public RiotService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<RiotService> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;

            var key = config.GetValue<string>("Riot:ApiKey");
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Riot:ApiKey não encontrado em configuração.");

            _apiKey = key.Trim();
            _logger.LogDebug("Riot API key length: {Len}", _apiKey.Length);
        }

        private HttpClient CreateClient()
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Remove("X-Riot-Token");
            client.DefaultRequestHeaders.Add("X-Riot-Token", _apiKey);
            return client;
        }

        private static async Task EnsureSuccessOrThrow(HttpResponseMessage res)
        {
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Riot API returned {(int)res.StatusCode} ({res.StatusCode}). Body: {body}");
            }
        }

        public async Task<RiotAccountDto?> GetAccountByRiotIdAsync(string regionalRoute, string gameName, string tagLine)
        {
            var client = CreateClient();
            var url = $"https://{regionalRoute}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
            var res = await client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrow(res);
            var stream = await res.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<RiotAccountDto>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<SummonerDto?> GetSummonerByPuuidAsync(string region, string puuid)
        {
            var client = CreateClient();
            var url = $"https://{region}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{Uri.EscapeDataString(puuid)}";
            var res = await client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrow(res);
            var stream = await res.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<SummonerDto>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<SummonerDto?> GetSummonerByNameAsync(string region, string summonerName)
        {
            var client = CreateClient();
            var url = $"https://{region}.api.riotgames.com/lol/summoner/v4/summoners/by-name/{Uri.EscapeDataString(summonerName)}";
            var res = await client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrow(res);
            var stream = await res.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<SummonerDto>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<LeagueEntryDto?> GetSoloQueueEntryAsync(string region, string summonerId)
        {
            var client = CreateClient();
            var url = $"https://{region}.api.riotgames.com/lol/league/v4/entries/by-summoner/{summonerId}";
            var res = await client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrow(res);
            var stream = await res.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<LeagueEntryDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list?.FirstOrDefault(e => e.QueueType == "RANKED_SOLO_5x5");
        }

        // novo: endpoint de diagnóstico para validar a API Key (retorna corpo da resposta)
        public async Task<LeagueEntryDto?> GetSoloQueueEntryByPuuidAsync(string region, string puuid)
        {
            var client = CreateClient();
            var url = $"https://{region}.api.riotgames.com/lol/league/v4/entries/by-puuid/{Uri.EscapeDataString(puuid)}";
            var res = await client.GetAsync(url);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrow(res);
            var stream = await res.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<LeagueEntryDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list?.FirstOrDefault(e => e.QueueType == "RANKED_SOLO_5x5");
        }

        public async Task<string> ValidateApiKeyAsync(string region = "br1")
        {
            var client = CreateClient();
            // chamamos o endpoint de status (v4) que deve responder com 200 se a chave for válida
            var url = $"https://{region}.api.riotgames.com/lol/status/v4/platform-data";
            var res = await client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();
            return $"Status: {(int)res.StatusCode} ({res.StatusCode}). Body: {body}";
        }
    }
}
