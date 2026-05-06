using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Services.SubscriptionServices;

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
                    Teams = item.Teams.Count,
                    item.StartDate,
                    item.EndDate
                })
                .ToListAsync();
            return Ok(leagues);
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
    }
}
