
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Models;
using NKZAPI.Services.LeagueServices;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/league")]
    public class LeagueController : ControllerBase
    {
        private readonly ILeagueInterface _leagueServices;
        public LeagueController(ILeagueInterface leagueServices)
        {
            _leagueServices = leagueServices;
        }

        [HttpGet("ListLeagues")]
        public async Task<ActionResult<List<League>>> ListLeaguesAsync()
        {
            var leagues = await _leagueServices.GetAllLeaguesAsync();
            return Ok(leagues);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> CreateLeagueAsync([FromBody] League league)
        {
            var response = await _leagueServices.AddLeagueAsync(league);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<League>> GetLeagueByIdAsync(Guid id)
        {
            var league = await _leagueServices.GetLeagueByIdAsync(id);
            if (league == null) return NotFound();
            return Ok(league);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateLeagueAsync(Guid id, [FromBody] League league)
        {
            league.Id = id;
            var response = await _leagueServices.UpdateLeagueAsync(league);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteLeagueAsync(Guid id)
        {
            var league = await _leagueServices.GetLeagueByIdAsync(id);
            if (league == null) return NotFound("League not found");
            await _leagueServices.DeleteLeagueAsync(league);
            return Ok("League deleted successfully");
        }
        [Authorize]
        [HttpPost("{leagueId:guid}/teams/{teamId:guid}")]
        public async Task<ActionResult> AddTeamToLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.AddTeamToLeagueAsync(leagueId, teamId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [Authorize]
        [HttpDelete("{leagueId:guid}/teams/{teamId:guid}")]
        public async Task<ActionResult> RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.RemoveTeamFromLeagueAsync(leagueId, teamId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("{leagueId:guid}/teams")]
        public async Task<ActionResult<List<Team>>> GetTeamsInLeagueAsync(Guid leagueId)
        {
            var teams = await _leagueServices.GetTeamsInLeagueAsync(leagueId);
            return Ok(teams);
        }

        [HttpGet("byTeam/{teamId:guid}")]
        public async Task<ActionResult<List<League>>> GetLeaguesByTeamIdAsync(Guid teamId)
        {
            var leagues = await _leagueServices.GetLeaguesByTeamIdAsync(teamId);
            return Ok(leagues);
        }
    }
}