using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.TeamServices;

namespace NKZAPI.Controllers
{        
    [ApiController]
    [Route("api/team")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamInterface _teamServices;
        public TeamController(ITeamInterface teamServices)
        {
            _teamServices = teamServices;
        }

        [HttpGet("ListTeams")]
        public async Task<ActionResult<List<Team>>> ListTeamsAsync()
        {
            var teams = await _teamServices.GetAllTeamsAsync();
            return Ok(teams);
        }

        [HttpPost("{id:guid}")]
        public async Task<ActionResult> CreateTeamAsync([FromBody] TeamDto team, Guid PlayerId)
        {
            var response = await _teamServices.AddTeamAsync(team, PlayerId);
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

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateTeamAsync(Guid id, [FromBody] TeamDto team)
        {
            team.Id = id;
            var response = await _teamServices.UpdateTeamAsync(team);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteTeamAsync(Guid id)
        {
            var team = await _teamServices.GetTeamByIdAsync(id);
            if (team == null)
            {
                return NotFound("Team not found");
            }
            await _teamServices.DeleteTeamAsync(team);
            return Ok("Team deleted successfully");
        }

        [HttpPost("{teamId:guid}/players")]
        public async Task<ActionResult<Player>> AddPlayerToTeamAsync(Guid teamId, [FromBody] Player player)
        {
            try
            {
                var added = await _teamServices.AddPlayerToTeamAsync(teamId, player);
                return Ok(added);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{teamId:guid}/players/{playerId:guid}")]
        public async Task<ActionResult> RemovePlayerFromTeamAsync(Guid teamId, Guid playerId)
        {
            try
            {
                await _teamServices.RemovePlayerFromTeamAsync(teamId, playerId);
                return Ok(new { message = "Player dissociated from team." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
