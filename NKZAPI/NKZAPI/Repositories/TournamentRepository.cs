using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Models;

namespace NKZAPI.Repositories
{
    public class TournamentRepository
    {
        private readonly NKZAPIContext _context;
        public TournamentRepository(NKZAPIContext context)
        {
            _context = context;
        }

        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .ToListAsync();
        }

        public async Task<Tournament?> GetTournamentByIdAsync(Guid id)
        {
            return await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tournament> AddTournamentAsync(Tournament tournament)
        {
            var entry = await _context.Set<Tournament>().AddAsync(tournament);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Tournament> UpdateTournamentAsync(Tournament tournament)
        {
            var dbTournament = await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == tournament.Id);
            if (dbTournament == null)
            {
                throw new DbUpdateConcurrencyException("The tournament was not found in the database.");
            }
            dbTournament.Name = tournament.Name;
            dbTournament.Prize = tournament.Prize;
            dbTournament.EntryFee = tournament.EntryFee;
            dbTournament.MaxTeams = tournament.MaxTeams;
            await _context.SaveChangesAsync();
            return dbTournament;
        }

        public async Task DeleteTournamentAsync(Tournament tournament)
        {
            _context.Set<Tournament>().Remove(tournament);
            await _context.SaveChangesAsync();
        }

        public async Task AddTeamToTournamentAsync(Guid tournamentId, Team team)
        {
            var tournament = await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
            {
                throw new DbUpdateConcurrencyException("The tournament was not found in the database.");
            }
            tournament.Teams.Add(team);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveTeamFromTournamentAsync(Guid tournamentId, Guid teamId)
        {
            var tournament = await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
            {
                throw new DbUpdateConcurrencyException("The tournament was not found in the database.");
            }
            var team = tournament.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                throw new DbUpdateConcurrencyException("The team was not found in the tournament.");
            }
            tournament.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Team>> GetTeamsInTournamentAsync(Guid tournamentId)
        {
            var tournament = await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            if (tournament == null)
            {
                throw new DbUpdateConcurrencyException("The tournament was not found in the database.");
            }
            return tournament.Teams;

        }

        public async Task<List<Tournament>> GetTournamentsByTeamIdAsync(Guid teamId)
        {
            return await _context.Set<Tournament>()
                .Include(t => t.Teams)
                .Where(t => t.Teams.Any(team => team.Id == teamId))
                .ToListAsync();
        }
    }
}
