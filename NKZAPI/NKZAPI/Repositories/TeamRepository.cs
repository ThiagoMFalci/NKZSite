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
        public async Task<List<Team>> GetTeamByPlayerIdAsync(Guid PlayerId)
        {
            var teams = await _context.Teams
                .Include(t => t.Players)
                .Where(t => t.Players.Any(p => p.Id == PlayerId))
                .ToListAsync();
            return teams;
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

        public async Task<Team> AddTeamWithOwnerPlayerAsync(Team team, Guid ownerUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var ownerPlayer = await _context.Players.FirstOrDefaultAsync(p => p.UserId == ownerUserId);
            if (ownerPlayer == null)
            {
                throw new InvalidOperationException("Owner player not found. Link a player before creating a team.");
            }

            if (ownerPlayer.TeamId != null)
            {
                throw new InvalidOperationException("Owner player is already in a team.");
            }

            var entry = await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();

            ownerPlayer.TeamId = entry.Entity.Id;
            ownerPlayer.IsCaptain = false;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return entry.Entity;
        }

        public async Task<Team?> UploadTeamImageAsync(Guid teamId, string imagePath)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return null;
            team.ProfileImageUrl = imagePath;
            await _context.SaveChangesAsync();
            return team;
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
            dbTeam.Tag = team.Tag;
            dbTeam.OwnerId = team.OwnerId;
            dbTeam.IsRecruiting = team.IsRecruiting;

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

        // Invitations
        public async Task<Invitation?> GetInvitationByIdAsync(Guid id)
        {
            return await _context.Set<Invitation>().FindAsync(id) as Invitation;
        }

        public async Task<List<Invitation>> GetInvitationsForPlayerAsync(Guid playerId)
        {
            return await _context.Set<Invitation>()
                .Where(i => i.PlayerId == playerId)
                .ToListAsync();
        }

        public async Task<List<Invitation>> GetInvitationsForTeamAsync(Guid teamId)
        {
            return await _context.Set<Invitation>()
                .Where(i => i.TeamId == teamId)
                .ToListAsync();
        }

        public async Task<Invitation> AddInvitationAsync(Invitation invitation)
        {
            var entry = await _context.Set<Invitation>().AddAsync(invitation);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task UpdateInvitationAsync(Invitation invitation)
        {
            var entry = _context.Entry(invitation);
            if (entry.State == EntityState.Detached)
            {
                _context.Set<Invitation>().Attach(invitation);
                entry = _context.Entry(invitation);
            }
            entry.State = EntityState.Modified;
            await _context.SaveChangesAsync();
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

        public async Task<Team?> UpdateRecruitingAsync(Guid teamId, bool isRecruiting)
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return null;

            team.IsRecruiting = isRecruiting;
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task RemovePlayerAsync(Player player)
        {
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
        }
    }
}
