using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.AuthServices;
using NKZAPI.Services.UserServices;
using System.Security.Claims;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/auth/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly IAuthInterface _authInterface;
        private readonly IUserInterface _userInterface;
        public UserController(UserServices userServices, IAuthInterface authInterface, IUserInterface userInterface)
        {
            _userServices = userServices;
            _authInterface = authInterface;
            _userInterface = userInterface;
        }
        [HttpGet("ListUsers")]
        public async Task<ActionResult<List<User>>> ListUsersAsync()
        {
            List<User> users = await _userServices.GetAllUsersAsync();
            return users;
        }
        [HttpPost("CreateUsers")]
        public async Task<ActionResult<User>> CreateUserAsync([FromBody] UserDto user)
        {
            var response = await _authInterface.UserAddAsync(user);
            return Ok(response);
        }
        [HttpPost("Login")]
        public async Task<ActionResult<User>> Login(UserLoginDto userLogin)
        {
            var response = await _authInterface.Login(userLogin);
            return Ok(response);
        }
        [Authorize]
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
        [Authorize]
        [HttpPost("players/{userId:guid}/sync/{summonerName}")]
        public async Task<ActionResult> SyncPlayerFromRiot(Guid userId, string summonerName, [FromQuery] string region = "br1")
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            if (callerId != userId && !isAdmin)
                return Forbid();

            var response = await _userInterface.UpdatePlayerFromRiotAsync(userId, summonerName, region);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

    }
}
