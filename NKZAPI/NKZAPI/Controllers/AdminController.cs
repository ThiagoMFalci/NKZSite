using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Services.SubscriptionServices;
using System.Security.Claims;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly NKZAPIContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public AdminController(NKZAPIContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var dashboard = new AdminDashboardDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalPlayers = await _context.Players.CountAsync(),
                TotalTeams = await _context.Teams.CountAsync(),
                TotalLeagues = await _context.Leagues.CountAsync(),
                ActiveSubscriptions = await _context.UserSubscriptions.CountAsync(item => item.Status == "Active" && item.EndsAt > now),
                SubscriptionRevenue = await _context.UserSubscriptions.Where(item => item.Status == "Active").SumAsync(item => item.Amount),
                WalletRevenue = await _context.WalletPayments.Where(item => item.Status == "Approved").SumAsync(item => item.Amount),
                LeagueRevenue = await _context.LeaguePayments.Where(item => item.Status == "Approved").SumAsync(item => item.Amount)
            };

            dashboard.RecentUsers = (await _context.Users
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .Select(item => new { item.Id, item.Email, item.Role, item.DiscordUsername, item.EmailVerified, item.DiscordVerified, item.CreatedAt })
                .ToListAsync()).Cast<object>().ToList();

            dashboard.RecentSubscriptions = (await _context.UserSubscriptions
                .Include(item => item.User)
                .Include(item => item.SubscriptionPlan)
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .Select(item => new
                {
                    item.Id,
                    item.UserId,
                    UserEmail = item.User != null ? item.User.Email : "",
                    PlanName = item.SubscriptionPlan != null ? item.SubscriptionPlan.Name : "",
                    item.Amount,
                    item.Status,
                    item.StartsAt,
                    item.EndsAt
                })
                .ToListAsync()).Cast<object>().ToList();

            dashboard.RecentPayments = (await _context.WalletPayments
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .Select(item => new { item.Id, item.UserId, item.Amount, item.Status, item.Provider, item.CreatedAt })
                .ToListAsync()).Cast<object>().ToList();

            return Ok(dashboard);
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetUsersAsync()
        {
            var users = await _context.Users
                .Include(item => item.Player)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.Email,
                    item.Role,
                    item.DiscordUsername,
                    item.DiscordVerified,
                    item.EmailVerified,
                    item.WalletBalance,
                    PlayerName = item.Player.Select(player => player.SummonerName).FirstOrDefault(),
                    item.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("users/{id}")]
        public async Task<ActionResult> UpdateUserAsync(Guid id, [FromBody] AdminUpdateUserDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == id);
            if (user is null) return NotFound(new { message = "Usuario nao encontrado." });

            var email = (request.Email ?? "").Trim().ToLowerInvariant();
            if (email.Length is < 5 or > 254 || !email.Contains('@'))
            {
                return BadRequest(new { message = "Email invalido." });
            }

            if (request.Role is not ("User" or "Admin"))
            {
                return BadRequest(new { message = "Perfil invalido." });
            }

            if (request.WalletBalance < 0)
            {
                return BadRequest(new { message = "Saldo nao pode ser negativo." });
            }

            var emailInUse = await _context.Users.AnyAsync(item => item.Id != id && item.Email == email);
            if (emailInUse) return BadRequest(new { message = "Este email ja esta em uso." });

            user.Email = email;
            user.Role = request.Role;
            user.DiscordUsername = SanitizeText(request.DiscordUsername, 80);
            user.DiscordVerified = request.DiscordVerified;
            user.EmailVerified = request.EmailVerified;
            user.EmailVerifiedAt = request.EmailVerified ? user.EmailVerifiedAt ?? DateTime.UtcNow : null;
            user.DiscordVerifiedAt = request.DiscordVerified ? user.DiscordVerifiedAt ?? DateTime.UtcNow : null;
            user.WalletBalance = request.WalletBalance;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuario atualizado." });
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUserAsync(Guid id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            if (Guid.TryParse(currentUserId, out var currentId) && currentId == id)
            {
                return BadRequest(new { message = "Voce nao pode excluir a propria conta administrativa logada." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == id);
            if (user is null) return NotFound(new { message = "Usuario nao encontrado." });

            await _context.Players.Where(item => item.UserId == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, (Guid?)null));
            await _context.Teams.Where(item => item.OwnerId == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OwnerId, (Guid?)null));

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuario excluido." });
        }

        [HttpGet("teams")]
        public async Task<ActionResult> GetTeamsAsync()
        {
            var teams = await _context.Teams
                .Include(item => item.Players)
                .OrderBy(item => item.Name)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Tag,
                    item.OwnerId,
                    item.Points,
                    item.IsRecruiting,
                    Players = item.Players != null ? item.Players.Count : 0
                })
                .ToListAsync();
            return Ok(teams);
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("teams/{id}")]
        public async Task<ActionResult> UpdateTeamAsync(Guid id, [FromBody] AdminUpdateTeamDto request)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(item => item.Id == id);
            if (team is null) return NotFound(new { message = "Time nao encontrado." });

            var name = SanitizeText(request.Name, 40);
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return BadRequest(new { message = "Nome do time invalido." });
            }

            var tag = SanitizeText(request.Tag, 5)?.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(tag) && (tag.Length < 3 || !tag.All(char.IsLetterOrDigit)))
            {
                return BadRequest(new { message = "A tag deve ter de 3 a 5 letras ou numeros." });
            }

            if (request.OwnerId.HasValue)
            {
                var ownerExists = await _context.Users.AnyAsync(item => item.Id == request.OwnerId.Value);
                if (!ownerExists) return BadRequest(new { message = "OwnerId nao pertence a nenhum usuario." });
            }

            team.Name = name;
            team.Tag = string.IsNullOrWhiteSpace(tag) ? null : tag;
            team.OwnerId = request.OwnerId;
            team.IsRecruiting = request.IsRecruiting;
            team.Points = Math.Max(0, request.Points);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Time atualizado." });
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("teams/{id}")]
        public async Task<ActionResult> DeleteTeamAsync(Guid id)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(item => item.Id == id);
            if (team is null) return NotFound(new { message = "Time nao encontrado." });

            await _context.Players.Where(item => item.TeamId == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.TeamId, (Guid?)null));
            await _context.LeagueQueueEntries.Where(item => item.TeamId == id).ExecuteDeleteAsync();
            await _context.LeagueStandings.Where(item => item.TeamId == id).ExecuteDeleteAsync();
            await _context.LeagueMatchReports.Where(item => item.TeamId == id || item.ReportedWinnerTeamId == id).ExecuteDeleteAsync();
            await _context.LeagueMatches
                .Where(item => item.TeamAId == id || item.TeamBId == id || item.WinnerTeamId == id || item.LoserTeamId == id || item.ProposedByTeamId == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.TeamAId, item => item.TeamAId == id ? null : item.TeamAId)
                    .SetProperty(item => item.TeamBId, item => item.TeamBId == id ? null : item.TeamBId)
                    .SetProperty(item => item.WinnerTeamId, item => item.WinnerTeamId == id ? null : item.WinnerTeamId)
                    .SetProperty(item => item.LoserTeamId, item => item.LoserTeamId == id ? null : item.LoserTeamId)
                    .SetProperty(item => item.ProposedByTeamId, item => item.ProposedByTeamId == id ? null : item.ProposedByTeamId));

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Time excluido." });
        }

        [HttpGet("leagues")]
        public async Task<ActionResult> GetLeaguesAsync()
        {
            var leagues = await _context.Leagues
                .Include(item => item.Teams)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Modality,
                    item.EntryFee,
                    item.Award,
                    item.MinimumTeamPoints,
                    item.MaximumTeamPoints,
                    item.MaxTeams,
                    item.MinimumElo,
                    item.MaximumElo,
                    item.ImageUrl,
                    item.RankingQueueOpenTime,
                    item.RankingQueueCloseTime,
                    Teams = item.Teams.Count,
                    item.StartDate,
                    item.EndDate
                })
                .ToListAsync();
            return Ok(leagues);
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("leagues/{id}")]
        public async Task<ActionResult> UpdateLeagueAsync(Guid id, [FromBody] AdminUpdateLeagueDto request)
        {
            var league = await _context.Leagues.FirstOrDefaultAsync(item => item.Id == id);
            if (league is null) return NotFound(new { message = "Liga nao encontrada." });

            var name = SanitizeText(request.Name, 60);
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return BadRequest(new { message = "Nome da liga invalido." });
            }

            if (request.Modality is not ("Ranking" or "Chaveamento"))
            {
                return BadRequest(new { message = "Modalidade invalida." });
            }

            if (request.EntryFee < 0 || request.Award < 0 || request.MinimumTeamPoints < 0 || request.MaximumTeamPoints < 0)
            {
                return BadRequest(new { message = "Valores numericos nao podem ser negativos." });
            }

            if (request.MaximumTeamPoints < request.MinimumTeamPoints)
            {
                return BadRequest(new { message = "Pontuacao maxima deve ser maior ou igual a minima." });
            }

            league.Name = name;
            league.Modality = request.Modality;
            league.EntryFee = request.EntryFee;
            league.Award = request.Award;
            league.MinimumTeamPoints = request.MinimumTeamPoints;
            league.MaximumTeamPoints = request.MaximumTeamPoints;
            league.MaxTeams = Math.Clamp(request.MaxTeams, 2, 64);
            league.MinimumElo = SanitizeText(request.MinimumElo, 24) ?? "UNRANKED";
            league.MaximumElo = SanitizeText(request.MaximumElo, 24) ?? "CHALLENGER";
            league.ImageUrl = SanitizeText(request.ImageUrl, 512);
            league.StartDate = request.StartDate;
            league.EndDate = request.EndDate;
            league.RankingQueueOpenTime = request.RankingQueueOpenTime;
            league.RankingQueueCloseTime = request.RankingQueueCloseTime;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Liga atualizada." });
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("leagues/{id}")]
        public async Task<ActionResult> DeleteLeagueAsync(Guid id)
        {
            var league = await _context.Leagues.FirstOrDefaultAsync(item => item.Id == id);
            if (league is null) return NotFound(new { message = "Liga nao encontrada." });

            var matchIds = await _context.LeagueMatches
                .Where(item => item.LeagueId == id)
                .Select(item => item.Id)
                .ToListAsync();

            await _context.LeagueQueueEntries.Where(item => item.LeagueId == id).ExecuteDeleteAsync();
            await _context.LeagueStandings.Where(item => item.LeagueId == id).ExecuteDeleteAsync();
            await _context.LeagueMatchReports.Where(item => matchIds.Contains(item.LeagueMatchId)).ExecuteDeleteAsync();
            await _context.LeagueMatches.Where(item => item.LeagueId == id).ExecuteDeleteAsync();

            _context.Leagues.Remove(league);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Liga excluida." });
        }

        [HttpGet("payments")]
        public async Task<ActionResult> GetPaymentsAsync()
        {
            var wallet = await _context.WalletPayments
                .OrderByDescending(item => item.CreatedAt)
                .Take(100)
                .Select(item => new { Kind = "Wallet", item.Id, item.UserId, item.Amount, item.Status, item.Provider, item.CreatedAt })
                .ToListAsync();

            var leagues = await _context.LeaguePayments
                .OrderByDescending(item => item.CreatedAt)
                .Take(100)
                .Select(item => new { Kind = "League", item.Id, UserId = item.CreatedByUserId, item.Amount, item.Status, item.Provider, item.CreatedAt })
                .ToListAsync();

            return Ok(wallet.Cast<object>().Concat(leagues.Cast<object>()).OrderByDescending(item => item.GetType().GetProperty("CreatedAt")?.GetValue(item)).Take(150));
        }

        [HttpGet("subscriptions")]
        public async Task<ActionResult> GetSubscriptionsAsync()
        {
            var subscriptions = await _context.UserSubscriptions
                .Include(item => item.User)
                .Include(item => item.SubscriptionPlan)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.UserId,
                    UserEmail = item.User != null ? item.User.Email : "",
                    PlanName = item.SubscriptionPlan != null ? item.SubscriptionPlan.Name : "",
                    item.Amount,
                    item.Status,
                    item.StartsAt,
                    item.EndsAt,
                    item.CreatedAt
                })
                .ToListAsync();
            return Ok(subscriptions);
        }

        [HttpGet("plans")]
        public async Task<ActionResult> GetPlansAsync()
        {
            return Ok(await _subscriptionService.ListPlansAsync(true));
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPost("plans")]
        public async Task<ActionResult> CreatePlanAsync([FromBody] CreateSubscriptionPlanDto request)
        {
            var response = await _subscriptionService.CreatePlanAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpPut("plans/{id}")]
        public async Task<ActionResult> UpdatePlanAsync(Guid id, [FromBody] CreateSubscriptionPlanDto request)
        {
            var response = await _subscriptionService.UpdatePlanAsync(id, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [EnableRateLimiting("GeneralWritePolicy")]
        [HttpDelete("plans/{id}")]
        public async Task<ActionResult> DeletePlanAsync(Guid id)
        {
            var response = await _subscriptionService.DeletePlanAsync(id);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        private static string? SanitizeText(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var sanitized = value
                .Replace("<", "", StringComparison.Ordinal)
                .Replace(">", "", StringComparison.Ordinal)
                .Trim();

            return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
        }
    }
}
