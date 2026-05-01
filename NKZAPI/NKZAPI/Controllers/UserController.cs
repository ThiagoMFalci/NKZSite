using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.AuthServices;
using NKZAPI.Services.UserServices;
using NKZAPI.Services.WalletServices;
using System.Security.Claims;
using System.Text.Json;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/auth/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly IAuthInterface _authInterface;
        private readonly IUserInterface _userInterface;
        private readonly IWalletService _walletService;
        public UserController(UserServices userServices, IAuthInterface authInterface, IUserInterface userInterface, IWalletService walletService)
        {
            _userServices = userServices;
            _authInterface = authInterface;
            _userInterface = userInterface;
            _walletService = walletService;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("ListUsers")]
        public async Task<ActionResult<List<UserPublicDto>>> ListUsersAsync()
        {
            List<User> users = await _userServices.GetAllUsersAsync();
            return users.Select(user => new UserPublicDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                DiscordUserId = user.DiscordUserId,
                DiscordUsername = user.DiscordUsername,
                DiscordVerified = user.DiscordVerified,
                CreatedAt = user.CreatedAt
            }).ToList();
        }
        [EnableRateLimiting("AuthPolicy")]
        [HttpPost("CreateUsers")]
        public async Task<ActionResult<User>> CreateUserAsync([FromBody] UserDto user)
        {
            var response = await _authInterface.UserAddAsync(user);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("VerifyDiscord")]
        public async Task<ActionResult> VerifyDiscordAsync([FromBody] DiscordVerificationDto verification)
        {
            var response = await _authInterface.VerifyDiscordAsync(verification);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("VerifyEmail")]
        public async Task<ActionResult> VerifyEmailAsync([FromBody] EmailVerificationDto verification)
        {
            var response = await _authInterface.VerifyEmailAsync(verification);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("ResendEmailVerification")]
        public async Task<ActionResult> ResendEmailVerificationAsync([FromBody] string email)
        {
            var response = await _authInterface.ResendEmailVerificationAsync(email);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("ResendDiscordVerification")]
        public async Task<ActionResult> ResendDiscordVerificationAsync([FromBody] string email)
        {
            var response = await _authInterface.ResendDiscordVerificationAsync(email);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("wallet")]
        public async Task<ActionResult> GetWalletAsync()
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId)) return Unauthorized();
            var response = await _walletService.GetWalletAsync(callerId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("wallet/deposit")]
        public async Task<ActionResult> CreateWalletDepositAsync([FromBody] WalletDepositDto deposit)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId)) return Unauthorized();
            var response = await _walletService.CreateDepositAsync(callerId, deposit);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("wallet/mercadopago/webhook")]
        public async Task<ActionResult> WalletMercadoPagoWebhookAsync()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault()
                ?? Request.Query["payment_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(paymentId) && Request.ContentLength.GetValueOrDefault() > 0)
            {
                using var document = await JsonDocument.ParseAsync(Request.Body);
                var root = document.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId)) paymentId = dataId.GetRawText().Trim('"');
                else if (root.TryGetProperty("id", out var id)) paymentId = id.GetRawText().Trim('"');
            }

            var response = await _walletService.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpGet("wallet/mercadopago/webhook")]
        public async Task<ActionResult> WalletMercadoPagoWebhookGetAsync()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault()
                ?? Request.Query["payment_id"].FirstOrDefault();
            var response = await _walletService.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("AuthPolicy")]
        [HttpPost("Login")]
        public async Task<ActionResult<User>> Login(UserLoginDto userLogin)
        {
            var response = await _authInterface.Login(userLogin);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("VerifyTwoFactor")]
        public async Task<ActionResult> VerifyTwoFactor(TwoFactorVerifyDto verification)
        {
            var response = await _authInterface.VerifyTwoFactorAsync(verification);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("ForgotPassword")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto request)
        {
            var response = await _authInterface.ForgotPasswordAsync(request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [EnableRateLimiting("VerificationPolicy")]
        [HttpPost("ResetPassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto request)
        {
            var response = await _authInterface.ResetPasswordAsync(request);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("UpdateUsers")]
        public async Task<ActionResult<UserDto>> UpdateUsers([FromBody] UserDto user, Guid id)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            if (callerId != id && !isAdmin)
                return Forbid();

            var response = await _userInterface.UpdateUserAsync(user, id);
            return Ok(response);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("DeleteUsers")]
        public async Task<ActionResult> DeleteUsers(Guid id)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            if (callerId != id && !isAdmin)
                return Forbid();

            var user = await _userServices.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            var i = await _userServices.DeleteUserAsync(id);
            return Ok(i);
        }


    }
}
