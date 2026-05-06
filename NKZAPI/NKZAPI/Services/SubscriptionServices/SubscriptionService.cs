using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NKZAPI.Services.SubscriptionServices
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly NKZAPIContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public SubscriptionService(NKZAPIContext context, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<Response<List<SubscriptionPlanDto>>> ListPlansAsync(bool includeInactive = false)
        {
            var plans = await _context.SubscriptionPlans
                .Where(item => includeInactive || item.IsActive)
                .OrderByDescending(item => item.IsFeatured)
                .ThenBy(item => item.SortOrder)
                .ThenBy(item => item.Price)
                .Select(item => ToPlanDto(item))
                .ToListAsync();

            return new Response<List<SubscriptionPlanDto>> { Success = true, Message = "Planos carregados.", Data = plans };
        }

        public async Task<Response<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanDto request)
        {
            var plan = new SubscriptionPlan
            {
                Name = Clean(request.Name),
                Description = Clean(request.Description),
                Price = request.Price,
                DurationMonths = request.DurationMonths,
                Benefits = Clean(request.Benefits),
                IsActive = request.IsActive,
                IsFeatured = request.IsFeatured,
                SortOrder = request.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return new Response<SubscriptionPlanDto> { Success = true, Message = "Plano criado.", Data = ToPlanDto(plan) };
        }

        public async Task<Response<SubscriptionPlanDto>> UpdatePlanAsync(Guid id, CreateSubscriptionPlanDto request)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == id);
            if (plan == null) return new Response<SubscriptionPlanDto> { Success = false, Message = "Plano nao encontrado." };

            plan.Name = Clean(request.Name);
            plan.Description = Clean(request.Description);
            plan.Price = request.Price;
            plan.DurationMonths = request.DurationMonths;
            plan.Benefits = Clean(request.Benefits);
            plan.IsActive = request.IsActive;
            plan.IsFeatured = request.IsFeatured;
            plan.SortOrder = request.SortOrder;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new Response<SubscriptionPlanDto> { Success = true, Message = "Plano atualizado.", Data = ToPlanDto(plan) };
        }

        public async Task<Response<string>> DeletePlanAsync(Guid id)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == id);
            if (plan == null) return new Response<string> { Success = false, Message = "Plano nao encontrado." };

            var hasSubscriptions = await _context.UserSubscriptions.AnyAsync(item => item.SubscriptionPlanId == id);
            if (hasSubscriptions)
            {
                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return new Response<string> { Success = true, Message = "Plano possui assinaturas e foi desativado.", Data = id.ToString() };
            }

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();
            return new Response<string> { Success = true, Message = "Plano excluido.", Data = id.ToString() };
        }

        public async Task<Response<UserSubscriptionDto?>> GetCurrentSubscriptionAsync(Guid userId)
        {
            var subscription = await _context.UserSubscriptions
                .Include(item => item.SubscriptionPlan)
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.EndsAt)
                .FirstOrDefaultAsync();

            return new Response<UserSubscriptionDto?> { Success = true, Message = "Assinatura carregada.", Data = subscription == null ? null : ToSubscriptionDto(subscription) };
        }

        public async Task<Response<string>> CreateCheckoutAsync(Guid userId, SubscriptionCheckoutDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return new Response<string> { Success = false, Message = "Usuario nao encontrado." };

            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive);
            if (plan == null) return new Response<string> { Success = false, Message = "Plano indisponivel." };

            var existing = await _context.UserSubscriptions
                .Where(item => item.UserId == userId && item.Status == "Active" && item.EndsAt > DateTime.UtcNow)
                .OrderByDescending(item => item.EndsAt)
                .FirstOrDefaultAsync();

            var startsAt = existing?.EndsAt > DateTime.UtcNow ? existing.EndsAt : DateTime.UtcNow;
            var subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                Amount = plan.Price,
                Status = plan.Price <= 0 ? "Active" : "Pending",
                StartsAt = startsAt,
                EndsAt = startsAt.AddMonths(plan.DurationMonths),
                ApprovedAt = plan.Price <= 0 ? DateTime.UtcNow : null
            };

            await _context.UserSubscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();

            if (plan.Price <= 0)
            {
                return new Response<string> { Success = true, Message = "Assinatura ativada.", Data = "" };
            }

            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new Response<string> { Success = false, Message = "Mercado Pago nao esta configurado. Defina MercadoPago__AccessToken." };
            }

            var checkout = await CreatePreferenceAsync(subscription, plan, user, accessToken);
            subscription.ProviderPreferenceId = checkout.PreferenceId;
            subscription.CheckoutUrl = checkout.Url;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new Response<string> { Success = true, Message = "Checkout de assinatura criado.", Data = subscription.CheckoutUrl };
        }

        public async Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return new Response<string> { Success = false, Message = "Payment id is required." };
            }

            var info = await GetMercadoPagoPaymentAsync(paymentId);
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var subscription = Guid.TryParse(info.ExternalReference, out var internalId)
                ? await _context.UserSubscriptions.FirstOrDefaultAsync(item => item.Id == internalId)
                : await _context.UserSubscriptions.FirstOrDefaultAsync(item => item.ProviderPaymentId == info.PaymentId);

            if (subscription == null)
            {
                return new Response<string> { Success = false, Message = "Assinatura nao encontrada." };
            }

            subscription.ProviderPaymentId = info.PaymentId;
            subscription.Status = ToLocalPaymentStatus(info.Status);
            subscription.UpdatedAt = DateTime.UtcNow;
            if (subscription.Status == "Active" && !subscription.ApprovedAt.HasValue)
            {
                subscription.ApprovedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new Response<string> { Success = true, Message = $"Assinatura atualizada: {subscription.Status}.", Data = subscription.Id.ToString() };
        }

        private async Task<(string PreferenceId, string Url)> CreatePreferenceAsync(UserSubscription subscription, SubscriptionPlan plan, User user, string accessToken)
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
                        ["title"] = $"Assinatura NKZ - {plan.Name}",
                        ["quantity"] = 1,
                        ["currency_id"] = "BRL",
                        ["unit_price"] = subscription.Amount
                    }
                },
                ["payer"] = new Dictionary<string, object?> { ["email"] = user.Email },
                ["external_reference"] = subscription.Id.ToString(),
                ["notification_url"] = $"{apiBaseUrl}/api/subscriptions/mercadopago/webhook",
                ["back_urls"] = new Dictionary<string, object?>
                {
                    ["success"] = $"{frontendBaseUrl}/subscriptions?payment=success",
                    ["failure"] = $"{frontendBaseUrl}/subscriptions?payment=failure",
                    ["pending"] = $"{frontendBaseUrl}/subscriptions?payment=pending"
                },
                ["auto_return"] = "approved"
            };

            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var result = await client.SendAsync(httpRequest);
            var body = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)result.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var preferenceId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
            var initPoint = root.TryGetProperty("init_point", out var initPointElement) ? initPointElement.GetString() : null;
            var sandboxInitPoint = root.TryGetProperty("sandbox_init_point", out var sandboxElement) ? sandboxElement.GetString() : null;
            var url = accessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(sandboxInitPoint) ? sandboxInitPoint : initPoint;
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
                ? "Active"
                : string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "Rejected"
                    : "Pending";
        }

        private static SubscriptionPlanDto ToPlanDto(SubscriptionPlan plan) => new()
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            DurationMonths = plan.DurationMonths,
            Benefits = plan.Benefits,
            IsActive = plan.IsActive,
            IsFeatured = plan.IsFeatured,
            SortOrder = plan.SortOrder
        };

        private static UserSubscriptionDto ToSubscriptionDto(UserSubscription subscription) => new()
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            PlanName = subscription.SubscriptionPlan?.Name ?? "",
            Amount = subscription.Amount,
            Status = subscription.Status,
            StartsAt = subscription.StartsAt,
            EndsAt = subscription.EndsAt,
            CheckoutUrl = subscription.CheckoutUrl
        };

        private static string Clean(string? value)
        {
            return (value ?? string.Empty).Trim().Replace("<", "").Replace(">", "");
        }

        private sealed record MercadoPagoPaymentInfo(string PaymentId, string Status, string? ExternalReference);
    }
}
