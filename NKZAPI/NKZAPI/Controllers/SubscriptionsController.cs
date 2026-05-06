using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NKZAPI.Dtos;
using NKZAPI.Services.SubscriptionServices;
using System.Security.Claims;
using System.Text.Json;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [Authorize]
        [HttpGet("plans")]
        public async Task<ActionResult> ListPlansAsync()
        {
            var includeInactive = User.IsInRole("Admin") && string.Equals(Request.Query["includeInactive"], "true", StringComparison.OrdinalIgnoreCase);
            var response = await _subscriptionService.ListPlansAsync(includeInactive);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentSubscriptionAsync()
        {
            if (!TryGetCallerId(out var callerId)) return Unauthorized();
            var response = await _subscriptionService.GetCurrentSubscriptionAsync(callerId);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("checkout")]
        public async Task<ActionResult> CreateCheckoutAsync([FromBody] SubscriptionCheckoutDto request)
        {
            if (!TryGetCallerId(out var callerId)) return Unauthorized();
            var response = await _subscriptionService.CreateCheckoutAsync(callerId, request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("mercadopago/webhook")]
        public async Task<ActionResult> MercadoPagoWebhookAsync()
        {
            var paymentId = await ResolvePaymentIdAsync(Request);
            var response = await _subscriptionService.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpGet("mercadopago/webhook")]
        public async Task<ActionResult> MercadoPagoWebhookGetAsync()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault()
                ?? Request.Query["payment_id"].FirstOrDefault();
            var response = await _subscriptionService.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        private bool TryGetCallerId(out Guid callerId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            return Guid.TryParse(callerIdClaim, out callerId);
        }

        private static async Task<string?> ResolvePaymentIdAsync(HttpRequest request)
        {
            var paymentId = request.Query["data.id"].FirstOrDefault()
                ?? request.Query["id"].FirstOrDefault()
                ?? request.Query["payment_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(paymentId) && request.ContentLength.GetValueOrDefault() > 0)
            {
                using var document = await JsonDocument.ParseAsync(request.Body);
                var root = document.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId)) paymentId = dataId.GetRawText().Trim('"');
                else if (root.TryGetProperty("id", out var id)) paymentId = id.GetRawText().Trim('"');
            }

            return paymentId;
        }
    }
}
