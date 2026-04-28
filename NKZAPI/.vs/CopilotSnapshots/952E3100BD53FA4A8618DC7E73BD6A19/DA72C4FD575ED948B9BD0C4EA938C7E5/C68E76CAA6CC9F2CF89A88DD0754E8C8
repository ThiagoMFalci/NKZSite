using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Models;

namespace NKZAPI.Repositories
{
    public class TeamRepository
    {
        private readonly NKZAPIContext _context;
        public TeamRepository(NKZAPIContext context)
        {
            _context = context;
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            return await _context.Teams
                .Include(t => t.Players)
                .ToListAsync();
        }

        public async Task<Team?> GetTeamByIdAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Team> AddTeamAsync(Team team)
        {
            var entry = await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<Team> UpdateTeamAsync(Team team)
        {
            // Atualiza apenas a entidade existente carregada pelo contexto
            var dbTeam = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == team.Id);

            if (dbTeam == null)
            {
                throw new DbUpdateConcurrencyException("The team was not found in the database.");
            }

            dbTeam.Name = team.Name;
            dbTeam.OwnerId = team.OwnerId;

            await _context.SaveChangesAsync();
            return dbTeam;
        }

        public async Task DeleteTeamAsync(Team team)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }

        // Métodos atômicos para Players (apenas acesso a dados)
        public async Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            return await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Player> AddPlayerAsync(Player player)
        {
            var entry = await _context.Players.AddAsync(player);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task UpdatePlayerAsync(Player player)
        {
            var entry = _context.Entry(player);
            if (entry.State == EntityState.Detached)
            {
                _context.Players.Attach(player);
                entry = _context.Entry(player);
            }
            entry.State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task RemovePlayerAsync(Player player)
        {
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
        }
    }
}
