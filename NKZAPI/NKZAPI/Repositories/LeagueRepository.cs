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
                .ToListAsync();
        }
        public async Task<League?> GetLeagueByIdAsync(Guid id)
        {
            return await _context.Leagues
                .Include(l => l.Teams)
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

    }
}
