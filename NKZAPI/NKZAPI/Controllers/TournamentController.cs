using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tournamentService.GetAllTournamentsAsync();
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _tournamentService.GetTournamentByIdAsync(id);
            if (!result.Success) return NotFound(result.Message);
            return Ok(result);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tournament tournament)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            if (!isAdmin || tournament.OwnerId == null)
                tournament.OwnerId = callerId;

            var result = await _tournamentService.AddTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(Get), new { id = Guid.Parse(result.Data) }, result);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament tournament)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            var existing = await _tournamentService.GetTournamentByIdAsync(id);
            if (!existing.Success || existing.Data == null) return NotFound(existing.Message);
            if (existing.Data.OwnerId != callerId && !isAdmin) return Forbid();

            // Garantir que o id da rota e o objeto sejam consistentes
            tournament.Id = id;
            var result = await _tournamentService.UpdateTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            var existing = await _tournamentService.GetTournamentByIdAsync(id);
            if (!existing.Success || existing.Data == null) return NotFound(existing.Message);
            if (existing.Data.OwnerId != callerId && !isAdmin) return Forbid();

            var tournament = new Tournament { Id = id };
            var result = await _tournamentService.DeleteTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> AddTeamToTournament(Guid tournamentId, Guid teamId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            var tournament = await _tournamentService.GetTournamentByIdAsync(tournamentId);
            if (!tournament.Success || tournament.Data == null) return NotFound(tournament.Message);

            var team = await _teamService.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            if (team.OwnerId != callerId && !isAdmin)
                return Forbid();

            var result = await _tournamentService.AddTeamToTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(Guid tournamentId, Guid teamId)
        {
            var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
            var tournament = await _tournamentService.GetTournamentByIdAsync(tournamentId);
            if (!tournament.Success || tournament.Data == null) return NotFound(tournament.Message);

            var team = await _teamService.GetTeamByIdAsync(teamId);
            if (team == null) return NotFound("Team not found");

            if (team.OwnerId != callerId && tournament.Data.OwnerId != callerId && !isAdmin)
                return Forbid();

            var result = await _tournamentService.RemoveTeamFromTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
    }
}
