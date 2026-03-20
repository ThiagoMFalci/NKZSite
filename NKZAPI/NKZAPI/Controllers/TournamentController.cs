using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Models;
using NKZAPI.Services.TournamentServices;
using NKZAPI.Services.TeamServices;
using System.Security.Claims;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentController : ControllerBase
    {
        private readonly ITournamentInterface _tournamentService;
        private readonly ITeamInterface _teamService;

        public TournamentController(ITournamentInterface tournamentService, ITeamInterface teamService)
        {
            _tournamentService = tournamentService;
            _teamService = teamService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tournamentService.GetAllTournamentsAsync();
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _tournamentService.GetTournamentByIdAsync(id);
            if (!result.Success) return NotFound(result.Message);
            return Ok(result);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tournament tournament)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (!isAdmin) return Forbid();

            var result = await _tournamentService.AddTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(Get), new { id = Guid.Parse(result.Data) }, result);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament tournament)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (!isAdmin) return Forbid();

            // Garantir que o id da rota e o objeto sejam consistentes
            tournament.Id = id;
            var result = await _tournamentService.UpdateTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (!isAdmin) return Forbid();

            var tournament = new Tournament { Id = id };
            var result = await _tournamentService.DeleteTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
        [Authorize]
        [HttpPost("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> AddTeamToTournament(Guid tournamentId, Guid teamId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            var team = await _teamService.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            if (team.OwnerId != callerId && !isAdmin)
                return Forbid();

            var result = await _tournamentService.AddTeamToTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
        [Authorize]
        [HttpDelete("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(Guid tournamentId, Guid teamId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");

            var team = await _teamService.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            if (team.OwnerId != callerId && !isAdmin)
                return Forbid();

            var result = await _tournamentService.RemoveTeamFromTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
    }
}
