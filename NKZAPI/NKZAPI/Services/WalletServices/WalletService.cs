using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NKZAPI.Services.WalletServices
{
    public class WalletService : IWalletService
    {
        private readonly NKZAPIContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public WalletService(NKZAPIContext context, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<Response<object>> GetWalletAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return new Response<object> { Success = false, Message = "Usuario nao encontrado." };

            var transactions = await _context.WalletTransactions
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.CreatedAt)
                .Take(20)
                .Select(item => new
                {
                    item.Id,
                    item.Amount,
                    item.Type,
                    item.Description,
                    item.CreatedAt
                })
                .ToListAsync();

            return new Response<object>
            {
                Success = true,
                Message = "Carteira carregada.",
                Data = new { balance = user.WalletBalance, transactions }
            };
        }

        public async Task<Response<string>> CreateDepositAsync(Guid userId, WalletDepositDto deposit)
        {
            if (deposit.Amount <= 0)
            {
                return new Response<string> { Success = false, Message = "Informe um valor maior que zero." };
            }

            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new Response<string> { Success = false, Message = "Mercado Pago nao esta configurado. Defina MercadoPago__AccessToken." };
            }

            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return new Response<string> { Success = false, Message = "Usuario nao encontrado." };

            var payment = new WalletPayment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = deposit.Amount,
                Status = "Pending"
            };
            await _context.WalletPayments.AddAsync(payment);
            await _context.SaveChangesAsync();

            var checkout = await CreatePreferenceAsync(payment, accessToken);
            payment.ProviderPreferenceId = checkout.PreferenceId;
            payment.CheckoutUrl = checkout.Url;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new Response<string>
            {
                Success = true,
                Message = "Checkout de recarga criado.",
                Data = payment.CheckoutUrl
            };
        }

        public async Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return new Response<string> { Success = false, Message = "Payment id is required." };
            }

            var info = await GetMercadoPagoPaymentAsync(paymentId);
            var payment = Guid.TryParse(info.ExternalReference, out var internalId)
                ? await _context.WalletPayments.FirstOrDefaultAsync(item => item.Id == internalId)
                : await _context.WalletPayments.FirstOrDefaultAsync(item => item.ProviderPaymentId == info.PaymentId);

            if (payment == null)
            {
                return new Response<string> { Success = false, Message = "Pagamento de carteira nao encontrado." };
            }

            payment.ProviderPaymentId = info.PaymentId;
            payment.Status = ToLocalPaymentStatus(info.Status);
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Status == "Approved" && !payment.ApprovedAt.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == payment.UserId);
                if (user == null) return new Response<string> { Success = false, Message = "Usuario nao encontrado." };

                user.WalletBalance += payment.Amount;
                payment.ApprovedAt = DateTime.UtcNow;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = payment.UserId,
                    Amount = payment.Amount,
                    Type = "Credit",
                    Description = "Recarga de carteira",
                    WalletPaymentId = payment.Id
                });
            }

            await _context.SaveChangesAsync();
            return new Response<string> { Success = true, Message = $"Pagamento atualizado: {payment.Status}.", Data = payment.Id.ToString() };
        }

        private async Task<(string PreferenceId, string Url)> CreatePreferenceAsync(WalletPayment payment, string accessToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var apiBaseUrl = (_configuration["Api:BaseUrl"] ?? _configuration["App:BaseUrl"])?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(apiBaseUrl) && httpContext != null)
            {
                apiBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            }

            var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var payload = new Dictionary<string, object?>
            {
                ["items"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["title"] = "Recarga de carteira NKZ",
                        ["quantity"] = 1,
                        ["currency_id"] = "BRL",
                        ["unit_price"] = payment.Amount
                    }
                },
                ["external_reference"] = payment.Id.ToString(),
                ["notification_url"] = $"{apiBaseUrl}/api/auth/User/wallet/mercadopago/webhook",
                ["back_urls"] = new Dictionary<string, object?>
                {
                    ["success"] = $"{frontendBaseUrl}/wallet?payment=success",
                    ["failure"] = $"{frontendBaseUrl}/wallet?payment=failure",
                    ["pending"] = $"{frontendBaseUrl}/wallet?payment=pending"
                },
                ["auto_return"] = "approved"
            };

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var result = await client.SendAsync(request);
            var body = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)result.StatusCode}: {body}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var preferenceId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
            var initPoint = root.TryGetProperty("init_point", out var initPointElement) ? initPointElement.GetString() : null;
            var sandboxInitPoint = root.TryGetProperty("sandbox_init_point", out var sandboxElement) ? sandboxElement.GetString() : null;
            var url = accessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(sandboxInitPoint)
                ? sandboxInitPoint
                : initPoint;

            if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Mercado Pago nao retornou URL de checkout.");
            return (preferenceId, url);
        }

        private async Task<MercadoPagoPaymentInfo> GetMercadoPagoPaymentAsync(string paymentId)
        {
            var accessToken = _configuration["MercadoPago:AccessToken"] ?? throw new InvalidOperationException("Mercado Pago nao configurado.");
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var result = await client.SendAsync(request);
            var body = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)result.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var id = root.TryGetProperty("id", out var idElement) ? idElement.GetRawText().Trim('"') : paymentId;
            var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? "" : "";
            var externalReference = root.TryGetProperty("external_reference", out var referenceElement) ? referenceElement.GetString() : null;
            return new MercadoPagoPaymentInfo(id, status, externalReference);
        }

        private static string ToLocalPaymentStatus(string? status)
        {
            return string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "Rejected"
                    : "Pending";
        }

        private sealed record MercadoPagoPaymentInfo(string PaymentId, string Status, string? ExternalReference);
    }
}
