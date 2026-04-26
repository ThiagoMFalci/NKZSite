using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.PlayerServices;
using NKZAPI.Services.UserServices;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/player")]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerInterface _playerInterface;
        public PlayerController( IPlayerInterface playerInterface)
        {
            _playerInterface = playerInterface;
        }

        [Authorize]
        [HttpPut("{userId:guid}/sync/{summonerName}")]
        public async Task<ActionResult> SyncPlayerFromRiot(Guid userId, string summonerName, [FromQuery] string region = "br1")
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized(new { message = "Token invalido ou sem identificador de usuario." });

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (callerId != userId && !isAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao pode sincronizar player de outro usuario." });

            var response = await _playerInterface.UpdatePlayerFromRiotAsync(userId, summonerName, region);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [Authorize]
        [HttpPost("{userId:guid}")]
        public async Task<ActionResult> AddPlayer(Guid userId, [FromBody] CreatePlayerDto player)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized(new { message = "Token invalido ou sem identificador de usuario." });

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (callerId != userId && !isAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao pode criar player para outro usuario." });

            if (string.IsNullOrWhiteSpace(player.SummonerName))
                return BadRequest(new { message = "SummonerName e obrigatorio." });

            var newPlayer = new Player
            {
                SummonerName = player.SummonerName.Trim(),
                RiotPuuid = player.RiotPuuid ?? "",
                SummonerLevel = player.SummonerLevel,
                MainRole = player.MainRole,
                LookingForTeam = player.LookingForTeam,
                SoloQueueTier = "UNRANKED",
                SoloQueueRank = "",
                LastStatsUpdate = DateTime.UtcNow,
                IsActive = true
            };

            var response = await _playerInterface.AddPlayerAsync(userId, newPlayer);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [HttpGet("{playerId:guid}")]
        public async Task<ActionResult> GetPlayerById(Guid playerId)
        {
            var response = await _playerInterface.GetPlayerByIdAsync(playerId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [HttpGet]
        public async Task<ActionResult> GetAllPlayers()
        {
            var response = await _playerInterface.GetAllPlayersAsync();
            if (!response.Success) return BadRequest(response);
            return Ok(response.Data);
        }
        [Authorize]
        [HttpDelete("{playerId:guid}")]
        public async Task<ActionResult> DeletePlayer(Guid playerId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");


            var getResponse = await _playerInterface.GetPlayerByIdAsync(playerId);
            if (!getResponse.Success || getResponse.Data == null)
                return BadRequest(getResponse);

            var player = getResponse.Data;
            if (player.UserId != callerId && !isAdmin)
                return Forbid();

            var response = await _playerInterface.DeletePlayerAsync(playerId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult> GetPlayerByUserId(Guid userId)
        {
            var response = await _playerInterface.GetPlayerByUserIdAsync(userId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPut("{userId:guid}/competitive-profile")]
        public async Task<ActionResult> UpdateCompetitiveProfile(Guid userId, [FromBody] PlayerCompetitiveProfileDto profile)
        {
            var response = await _playerInterface.UpdateCompetitiveProfileAsync(userId, profile);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(response);
            if (!response.Success && response.Message == "Forbidden") return Forbid();
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("{userId}/profile-image")]
        public async Task<ActionResult> UploadProfileImage(Guid userId, IFormFile image)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            if (callerId != userId && !isAdmin)
                return Forbid();

            var user = await _playerInterface.GetPlayerByUserIdAsync(userId);
            if (!user.Success)
            {
                return NotFound("Player not found");
            }
            var i = await _playerInterface.UploadProfileImage(userId, image);
            return Ok(i);
        }

    }
}
