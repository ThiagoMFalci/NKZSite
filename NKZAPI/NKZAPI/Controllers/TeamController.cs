using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.PlayerServices;
using NKZAPI.Services.TeamServices;

namespace NKZAPI.Controllers
{        
    [ApiController]
    [Route("api/team")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ITeamInterface _teamServices;
        public TeamController(ITeamInterface teamServices)
        {
            _teamServices = teamServices;
        }

        [Authorize]
        [HttpPost("{teamId:guid}/players/{playerId:guid}/expel")]
        public async Task<ActionResult> ExpelPlayerAsync(Guid teamId, Guid playerId)
        {
            var team = await _teamServices.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            var response = await _teamServices.ExpelPlayerAsync(teamId, playerId);

            if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });

            if (!response.Success && response.Message == "Forbidden")
                return Forbid();

            if (!response.Success && response.Message != null && response.Message.Contains("not found"))
                return NotFound(new { message = response.Message });

            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("ListTeams")]
        public async Task<ActionResult<List<Team>>> ListTeamsAsync()
        {
            var teams = await _teamServices.GetAllTeamsAsync();
            return Ok(teams);
        }
        [Authorize]
        [HttpPost("{id:guid}")]
        public async Task<ActionResult> CreateTeamAsync([FromBody] TeamDto team, Guid PlayerId)
        {
            var response = await _teamServices.AddTeamAsync(team, PlayerId);

            if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });

            if (!response.Success && response.Message == "Forbidden")
                return Forbid();

            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Team>> GetTeamByIdAsync(Guid id)
        {
            var team = await _teamServices.GetTeamByIdAsync(id);
            if (team == null) return NotFound();
            return Ok(team);
        }
        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateTeamAsync(Guid id, [FromBody] TeamDto team)
        {
            // Ensure route and body id match
            team.Id = id;

            var response = await _teamServices.UpdateTeamAsync(team);

            if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });

            if (!response.Success && response.Message == "Forbidden")
                return Forbid();

            if (!response.Success && response.Message != null && response.Message.Contains("not found"))
                return NotFound(new { message = response.Message });

            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteTeamAsync(Guid id)
        {
            var existing = await _teamServices.GetTeamByIdAsync(id);
            if (existing == null)
            {
                return NotFound("Team not found");
            }

            var deleteResponse = await _teamServices.DeleteTeamAsync(existing);

            if (!deleteResponse.Success && deleteResponse.Message == "Unauthorized")
                return Unauthorized(new { message = deleteResponse.Message });

            if (!deleteResponse.Success && deleteResponse.Message == "Forbidden")
                return Forbid();

            if (!deleteResponse.Success) return BadRequest(deleteResponse);

            return Ok(deleteResponse);
        }
        [Authorize]
        [HttpPost("{teamId:guid}/players")]
        public async Task<ActionResult<Player>> AddPlayerToTeamAsync(Guid teamId, [FromBody] Guid playerId)
        {
            var team = await _teamServices.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            var addResponse = await _teamServices.AddPlayerToTeamAsync(teamId, playerId);

            if (!addResponse.Success && addResponse.Message == "Unauthorized")
                return Unauthorized(new { message = addResponse.Message });

            if (!addResponse.Success && addResponse.Message == "Forbidden")
                return Forbid();

            if (!addResponse.Success) return BadRequest(addResponse);

            return Ok(addResponse);
        }
        [Authorize]
        [HttpPost("{teamId:guid}/invitations")]
        public async Task<ActionResult> CreateInvitationAsync(Guid teamId, [FromBody] Invitation invitation)
        {
            // Ensure teamId matches body
            invitation.TeamId = teamId;

            var response = await _teamServices.CreateInvitationAsync(invitation);

            if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });

            if (!response.Success && response.Message == "Forbidden")
                return Forbid();

            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }
        [Authorize]
        [HttpGet("{teamId:guid}/invitations")]
        public async Task<ActionResult<List<Invitation>>> GetTeamInvitationsAsync(Guid teamId)
        {
            var list = await _teamServices.GetInvitationsForTeamAsync(teamId);
            return Ok(list);
        }
        [Authorize]
        [HttpGet("players/{playerId:guid}/invitations")]
        public async Task<ActionResult<List<Invitation>>> GetPlayerInvitationsAsync(Guid playerId)
        {
            var list = await _teamServices.GetInvitationsForPlayerAsync(playerId);
            return Ok(list);
        }
        [Authorize]
        [HttpPost("invitations/{invitationId:guid}/respond")]
        public async Task<ActionResult> RespondToInvitationAsync(Guid invitationId, [FromBody] bool accept)
        {
            var response = await _teamServices.RespondToInvitationAsync(invitationId, accept);

            if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });

            if (!response.Success && response.Message == "Forbidden")
                return Forbid();

            if (!response.Success && response.Message != null && response.Message.Contains("not found"))
                return NotFound(new { message = response.Message });

            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }
        [Authorize]
        [HttpDelete("{teamId:guid}/players/{playerId:guid}")]
        public async Task<ActionResult> RemovePlayerFromTeamAsync(Guid teamId, Guid playerId)
        {
            var team = await _teamServices.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            var removeResponse = await _teamServices.RemovePlayerFromTeamAsync(teamId, playerId);

            if (!removeResponse.Success && removeResponse.Message == "Unauthorized")
                return Unauthorized(new { message = removeResponse.Message });

            if (!removeResponse.Success && removeResponse.Message == "Forbidden")
                return Forbid();

            if (!removeResponse.Success) return BadRequest(removeResponse);

            return Ok(removeResponse);
        }
        [HttpPost("{teamId:guid}/players/{playerId:guid}/IsCaptain/{i:bool}")]
        public async Task<ActionResult> IsCaptain(Guid teamId, Guid playerId, bool i)
        {
            var response = await _teamServices.AssignCaptainAsync(teamId, playerId, i);
             if (!response.Success && response.Message == "Unauthorized")
                return Unauthorized(new { message = response.Message });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
