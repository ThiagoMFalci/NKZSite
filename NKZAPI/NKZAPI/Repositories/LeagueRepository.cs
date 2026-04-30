using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Models;

namespace NKZAPI.Repositories
{
    public class LeagueRepository
    {
        private readonly NKZAPIContext _context;
        public LeagueRepository(NKZAPIContext context)
        {
            _context = context;
        }

        public async Task<List<League>> GetAllLeaguesAsync()
        {
            return await _context.Leagues
                .Include(l => l.Teams)
                    .ThenInclude(t => t.Players)
                .ToListAsync();
        }
        public async Task<League?> GetLeagueByIdAsync(Guid id)
        {
            return await _context.Leagues
                .Include(l => l.Teams)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.MatchHistory)
                .Include(l => l.Matches)
                    .ThenInclude(m => m.Reports)
                .Include(l => l.Standings)
                .Include(l => l.QueueEntries)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
        public async Task<League> AddLeagueAsync(League league)
        {
            var entry = await _context.Leagues.AddAsync(league);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
        public async Task<League> UpdateLeagueAsync(League league)
        {
            var dbLeague = await _context.Leagues
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.Id == league.Id);
            if (dbLeague == null)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }
            dbLeague.Name = league.Name;
            dbLeague.Award = league.Award;
            dbLeague.EntryFee = league.EntryFee;
            dbLeague.ImageUrl = league.ImageUrl;
            dbLeague.MaxTeams = league.MaxTeams;
            dbLeague.MinimumElo = league.MinimumElo;
            dbLeague.MaximumElo = league.MaximumElo;
            dbLeague.MinimumTeamPoints = league.MinimumTeamPoints;
            dbLeague.MaximumTeamPoints = league.MaximumTeamPoints;
            dbLeague.RankingQueueOpenTime = league.RankingQueueOpenTime;
            dbLeague.RankingQueueCloseTime = league.RankingQueueCloseTime;
            dbLeague.StartDate = league.StartDate;
            dbLeague.EndDate = league.EndDate;
            dbLeague.Modality = league.Modality;
            await _context.SaveChangesAsync();
            return dbLeague;
        }
        public async Task<League> UploadLeagueImageAsync(Guid leagueId, string imageUrl)
        {
            var dbLeague = await _context.Leagues.FirstOrDefaultAsync(l => l.Id == leagueId);
            if (dbLeague == null)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }

            dbLeague.ImageUrl = imageUrl;
            await _context.SaveChangesAsync();
            return dbLeague;
        }
        public async Task DeleteLeagueAsync(League league)
        {
            _context.Leagues.Remove(league);
            await _context.SaveChangesAsync();
        }
        public async Task AddTeamToLeagueAsync(Guid leagueId, Team team)
        {
            var leagueExists = await _context.Leagues.AnyAsync(l => l.Id == leagueId);
            if (!leagueExists)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE ""Teams"" SET ""LeagueId"" = {leagueId} WHERE ""Id"" = {team.Id}");
            if (affected == 0)
            {
                throw new DbUpdateConcurrencyException("The team was not found in the database.");
            }

            var hasStanding = await _context.LeagueStandings.AnyAsync(standing => standing.LeagueId == leagueId && standing.TeamId == team.Id);
            if (!hasStanding)
            {
                await _context.LeagueStandings.AddAsync(new LeagueStanding
                {
                    Id = Guid.NewGuid(),
                    LeagueId = leagueId,
                    TeamId = team.Id
                });
                await _context.SaveChangesAsync();
            }
        }
        public async Task RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId)
        {
            var leagueExists = await _context.Leagues.AnyAsync(l => l.Id == leagueId);
            if (!leagueExists)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE ""Teams"" SET ""LeagueId"" = NULL WHERE ""Id"" = {teamId} AND ""LeagueId"" = {leagueId}");
            if (affected == 0)
            {
                throw new DbUpdateConcurrencyException("The team was not found in the league.");
            }

            var standings = await _context.LeagueStandings
                .Where(standing => standing.LeagueId == leagueId && standing.TeamId == teamId)
                .ToListAsync();
            _context.LeagueStandings.RemoveRange(standings);
            var queueEntries = await _context.LeagueQueueEntries
                .Where(entry => entry.LeagueId == leagueId && entry.TeamId == teamId && entry.Status == "Waiting")
                .ToListAsync();
            _context.LeagueQueueEntries.RemoveRange(queueEntries);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Team>> GetTeamsInLeagueAsync(Guid leagueId)
        {
            var league = await _context.Leagues
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.Id == leagueId);
            if (league == null)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }
            return league.Teams;

        }
        public async Task<List<League>> GetLeaguesByTeamIdAsync(Guid teamId)
        {
            return await _context.Leagues
                .Include(l => l.Teams)
                .Where(l => l.Teams.Any(t => t.Id == teamId))
                .ToListAsync();
        }

        public async Task<List<LeagueMatch>> GetLeagueMatchesAsync(Guid leagueId)
        {
            return await _context.LeagueMatches
                .Include(match => match.Reports)
                .Where(match => match.LeagueId == leagueId)
                .OrderBy(match => match.WeekNumber)
                .ThenBy(match => match.Bracket)
                .ThenBy(match => match.MatchNumber)
                .ToListAsync();
        }

        public async Task<LeagueMatch?> GetLeagueMatchByIdAsync(Guid matchId)
        {
            return await _context.LeagueMatches
                .Include(match => match.Reports)
                .FirstOrDefaultAsync(match => match.Id == matchId);
        }

        public async Task ReplacePlayoffAsync(Guid leagueId, List<LeagueMatch> matches, List<LeagueStanding> standings)
        {
            var currentMatches = await _context.LeagueMatches.Where(match => match.LeagueId == leagueId).ToListAsync();
            var currentStandings = await _context.LeagueStandings.Where(standing => standing.LeagueId == leagueId).ToListAsync();
            var matchIds = currentMatches.Select(match => match.Id).ToList();
            var currentReports = await _context.LeagueMatchReports
                .Where(report => matchIds.Contains(report.LeagueMatchId))
                .ToListAsync();

            _context.LeagueMatchReports.RemoveRange(currentReports);
            _context.LeagueMatches.RemoveRange(currentMatches);
            _context.LeagueStandings.RemoveRange(currentStandings);
            await _context.LeagueMatches.AddRangeAsync(matches);
            await _context.LeagueStandings.AddRangeAsync(standings);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLeagueMatchAsync(LeagueMatch match)
        {
            _context.LeagueMatches.Update(match);
            await _context.SaveChangesAsync();
        }

        public async Task<LeagueStanding?> GetStandingAsync(Guid leagueId, Guid teamId)
        {
            return await _context.LeagueStandings.FirstOrDefaultAsync(standing => standing.LeagueId == leagueId && standing.TeamId == teamId);
        }

        public async Task<LeagueMatchReport?> GetMatchReportAsync(Guid matchId, Guid teamId)
        {
            return await _context.LeagueMatchReports.FirstOrDefaultAsync(report => report.LeagueMatchId == matchId && report.TeamId == teamId);
        }

        public async Task AddMatchReportAsync(LeagueMatchReport report)
        {
            await _context.LeagueMatchReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<LeagueQueueEntry?> GetWaitingQueueEntryAsync(Guid leagueId, Guid teamId)
        {
            return await _context.LeagueQueueEntries
                .FirstOrDefaultAsync(entry => entry.LeagueId == leagueId && entry.TeamId == teamId && entry.Status == "Waiting");
        }

        public async Task<List<LeagueQueueEntry>> GetWaitingQueueEntriesAsync(Guid leagueId)
        {
            return await _context.LeagueQueueEntries
                .Where(entry => entry.LeagueId == leagueId && entry.Status == "Waiting")
                .OrderBy(entry => entry.JoinedAt)
                .ToListAsync();
        }

        public async Task<LeagueQueueEntry> AddQueueEntryAsync(LeagueQueueEntry entry)
        {
            var added = await _context.LeagueQueueEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task<LeaguePayment?> GetApprovedLeaguePaymentAsync(Guid leagueId, Guid teamId)
        {
            return await _context.LeaguePayments
                .Where(payment => payment.LeagueId == leagueId && payment.TeamId == teamId && payment.Status == "Approved")
                .OrderByDescending(payment => payment.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<LeaguePayment?> GetPendingLeaguePaymentAsync(Guid leagueId, Guid teamId)
        {
            return await _context.LeaguePayments
                .Where(payment => payment.LeagueId == leagueId && payment.TeamId == teamId && payment.Status == "Pending")
                .OrderByDescending(payment => payment.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<LeaguePayment?> GetLeaguePaymentByIdAsync(Guid paymentId)
        {
            return await _context.LeaguePayments.FirstOrDefaultAsync(payment => payment.Id == paymentId);
        }

        public async Task<LeaguePayment?> GetLeaguePaymentByProviderPaymentIdAsync(string providerPaymentId)
        {
            return await _context.LeaguePayments.FirstOrDefaultAsync(payment => payment.ProviderPaymentId == providerPaymentId);
        }

        public async Task<List<LeaguePayment>> GetApprovedLeaguePaymentsByLeagueAsync(Guid leagueId)
        {
            return await _context.LeaguePayments
                .Where(payment => payment.LeagueId == leagueId && payment.Status == "Approved")
                .ToListAsync();
        }

        public async Task<LeaguePayment> AddLeaguePaymentAsync(LeaguePayment payment)
        {
            var added = await _context.LeaguePayments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task<LeagueMatch> AddMatchAsync(LeagueMatch match)
        {
            var added = await _context.LeagueMatches.AddAsync(match);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
