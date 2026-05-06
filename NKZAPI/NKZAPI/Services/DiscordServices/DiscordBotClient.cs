using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NKZAPI.Dtos;

namespace NKZAPI.Services.DiscordServices
{
    public class DiscordBotClient : IDiscordVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DiscordBotClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<DiscordVerificationDeliveryDto> SendVerificationCodeAsync(string discordIdentity, string email, string code)
        {
            var baseUrl = _configuration["DiscordBot:BaseUrl"];
            var secret = _configuration["DiscordBot:Secret"];
            var timeoutSeconds = int.TryParse(_configuration["DiscordBot:TimeoutSeconds"], out var configuredTimeout)
                ? Math.Clamp(configuredTimeout, 5, 60)
                : 10;

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Discord bot integration is not configured.");
            }

            var payload = JsonSerializer.Serialize(new
            {
                discordIdentity,
                email,
                code,
                serverInviteUrl = _configuration["Discord:ServerInviteUrl"]
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/verification/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
            var body = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
                    ? "Discord bot could not send the verification code."
                    : body);
            }

            return JsonSerializer.Deserialize<DiscordVerificationDeliveryDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DiscordVerificationDeliveryDto();
        }
    }
}
