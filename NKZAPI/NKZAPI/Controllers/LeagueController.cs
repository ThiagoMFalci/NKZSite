
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.LeagueServices;
using System.Text.Json;

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

        [Authorize]
        [HttpGet("ListLeagues")]
        public async Task<ActionResult<List<LeaguePublicDto>>> ListLeaguesAsync()
        {
            var leagues = await _leagueServices.GetAllLeaguesAsync();
            return Ok(leagues.Select(league => league.ToPublicDto(User)).ToList());
        }
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost]
        public async Task<ActionResult> CreateLeagueAsync([FromBody] League league)
        {
            var response = await _leagueServices.AddLeagueAsync(league);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("UploadPolicy")]
        [HttpPost("{leagueId:guid}/image")]
        public async Task<ActionResult> UploadLeagueImageAsync(Guid leagueId, IFormFile image)
        {
            var league = await _leagueServices.GetLeagueByIdAsync(leagueId);
            if (league == null) return NotFound("League not found");

            var response = await _leagueServices.UploadLeagueImageAsync(leagueId, image);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<LeaguePublicDto>> GetLeagueByIdAsync(Guid id)
        {
            var league = await _leagueServices.GetLeagueByIdAsync(id);
            if (league == null) return NotFound();
            return Ok(league.ToPublicDto(User));
        }
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateLeagueAsync(Guid id, [FromBody] League league)
        {
            league.Id = id;
            var response = await _leagueServices.UpdateLeagueAsync(league);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteLeagueAsync(Guid id)
        {
            var league = await _leagueServices.GetLeagueByIdAsync(id);
            if (league == null) return NotFound("League not found");
            await _leagueServices.DeleteLeagueAsync(league);
            return Ok("League deleted successfully");
        }
        [Authorize]
        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("{leagueId:guid}/teams/{teamId:guid}")]
        public async Task<ActionResult> AddTeamToLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.AddTeamToLeagueAsync(leagueId, teamId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para usar este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("payments/{paymentId:guid}/confirm")]
        public async Task<ActionResult> ConfirmLeaguePaymentAsync(Guid paymentId)
        {
            var response = await _leagueServices.ConfirmLeaguePaymentAsync(paymentId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpPost("payments/mercadopago/webhook")]
        public async Task<ActionResult> MercadoPagoWebhookAsync()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault()
                ?? Request.Query["payment_id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(paymentId) && Request.ContentLength.GetValueOrDefault() > 0)
            {
                using var document = await JsonDocument.ParseAsync(Request.Body);
                var root = document.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId))
                {
                    paymentId = dataId.GetRawText().Trim('"');
                }
                else if (root.TryGetProperty("id", out var id))
                {
                    paymentId = id.GetRawText().Trim('"');
                }
            }

            var response = await _leagueServices.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("PaymentPolicy")]
        [HttpGet("payments/mercadopago/webhook")]
        public async Task<ActionResult> MercadoPagoWebhookGetAsync()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault()
                ?? Request.Query["payment_id"].FirstOrDefault();
            var response = await _leagueServices.ProcessMercadoPagoNotificationAsync(paymentId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("{leagueId:guid}/teams/{teamId:guid}")]
        public async Task<ActionResult> RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.RemoveTeamFromLeagueAsync(leagueId, teamId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para usar este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("{leagueId:guid}/teams")]
        public async Task<ActionResult<List<TeamPublicDto>>> GetTeamsInLeagueAsync(Guid leagueId)
        {
            var teams = await _leagueServices.GetTeamsInLeagueAsync(leagueId);
            return Ok(teams.Select(team => team.ToPublicDto()).ToList());
        }

        [Authorize]
        [HttpGet("byTeam/{teamId:guid}")]
        public async Task<ActionResult<List<LeaguePublicDto>>> GetLeaguesByTeamIdAsync(Guid teamId)
        {
            var leagues = await _leagueServices.GetLeaguesByTeamIdAsync(teamId);
            return Ok(leagues.Select(league => league.ToPublicDto(User)).ToList());
        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("{leagueId:guid}/playoff/generate")]
        public async Task<ActionResult> GeneratePlayoffAsync(Guid leagueId)
        {
            var response = await _leagueServices.GeneratePlayoffAsync(leagueId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("matches/{matchId:guid}/complete")]
        public async Task<ActionResult> CompleteMatchAsync(Guid matchId, [FromBody] LeagueMatchResultDto result)
        {
            var response = await _leagueServices.CompleteMatchAsync(matchId, result);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("UploadPolicy")]
        [HttpPost("matches/{matchId:guid}/report")]
        public async Task<ActionResult> SubmitMatchReportAsync(Guid matchId, [FromForm] Guid reportedWinnerTeamId, [FromForm] IFormFile proofImage)
        {
            var response = await _leagueServices.SubmitMatchReportAsync(matchId, reportedWinnerTeamId, proofImage);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para reportar por este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("{leagueId:guid}/queue/{teamId:guid}")]
        public async Task<ActionResult> JoinRankingQueueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.JoinRankingQueueAsync(leagueId, teamId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para usar este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("{leagueId:guid}/queue/{teamId:guid}")]
        public async Task<ActionResult> LeaveRankingQueueAsync(Guid leagueId, Guid teamId)
        {
            var response = await _leagueServices.LeaveRankingQueueAsync(leagueId, teamId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para usar este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("matches/{matchId:guid}/schedule/propose")]
        public async Task<ActionResult> ProposeMatchScheduleAsync(Guid matchId, [FromBody] LeagueMatchScheduleProposalDto proposal)
        {
            var response = await _leagueServices.ProposeMatchScheduleAsync(matchId, proposal);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Voce nao tem permissao para sugerir horario por este time." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("matches/{matchId:guid}/schedule/accept")]
        public async Task<ActionResult> AcceptMatchScheduleAsync(Guid matchId)
        {
            var response = await _leagueServices.AcceptMatchScheduleAsync(matchId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Apenas o outro time pode confirmar este horario." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("matches/{matchId:guid}/schedule/reject")]
        public async Task<ActionResult> RejectMatchScheduleAsync(Guid matchId)
        {
            var response = await _leagueServices.RejectMatchScheduleAsync(matchId);
            if (!response.Success && response.Message == "Unauthorized") return Unauthorized(new { message = response.Message });
            if (!response.Success && response.Message == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { message = "Apenas o outro time pode recusar este horario." });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
