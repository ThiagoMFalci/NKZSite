using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Models;
using NKZAPI.Services.TournamentServices;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentController : ControllerBase
    {
        private readonly ITournamentInterface _tournamentService;

        public TournamentController(ITournamentInterface tournamentService)
        {
            _tournamentService = tournamentService;
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
            var result = await _tournamentService.AddTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(Get), new { id = Guid.Parse(result.Data) }, result);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tournament tournament)
        {
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
            var tournament = new Tournament { Id = id };
            var result = await _tournamentService.DeleteTournamentAsync(tournament);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
        [Authorize]
        [HttpPost("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> AddTeamToTournament(Guid tournamentId, Guid teamId)
        {
            var result = await _tournamentService.AddTeamToTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
        [Authorize]
        [HttpDelete("{tournamentId}/teams/{teamId}")]
        public async Task<IActionResult> RemoveTeamFromTournament(Guid tournamentId, Guid teamId)
        {
            var result = await _tournamentService.RemoveTeamFromTournamentAsync(tournamentId, teamId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
    }
}
