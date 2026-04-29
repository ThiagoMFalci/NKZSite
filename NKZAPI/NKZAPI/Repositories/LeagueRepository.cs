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
            dbLeague.MaxTeams = league.MaxTeams;
            dbLeague.MinimumElo = league.MinimumElo;
            dbLeague.MaximumElo = league.MaximumElo;
            dbLeague.StartDate = league.StartDate;
            dbLeague.EndDate = league.EndDate;
            dbLeague.Modality = league.Modality;
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
            var league = await _context.Leagues
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.Id == leagueId);
            if (league == null)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }
            league.Teams.Add(team);
            await _context.SaveChangesAsync();
        }
        public async Task RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId)
        {
            var league = await _context.Leagues
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.Id == leagueId);
            if (league == null)
            {
                throw new DbUpdateConcurrencyException("The league was not found in the database.");
            }
            var team = league.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                throw new DbUpdateConcurrencyException("The team was not found in the league.");
            }
            league.Teams.Remove(team);
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

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
