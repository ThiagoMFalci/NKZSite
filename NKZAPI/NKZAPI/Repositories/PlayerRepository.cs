using NKZAPI.Data;
using NKZAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace NKZAPI.Repositories

{
    public class PlayerRepository
    {
        private readonly NKZAPIContext _playerRepository;
        public PlayerRepository(NKZAPIContext context)
        {
            _playerRepository = context;
        }


        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _playerRepository.Players
                .Include(p => p.ChampionStats)
                .Include(p => p.RoleStats)
                .Include(p => p.MatchHistory)
                .ToListAsync();
        }

        public async Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            return await _playerRepository.Players
                .Include(p => p.ChampionStats)
                .Include(p => p.RoleStats)
                .Include(p => p.MatchHistory)
                .FirstOrDefaultAsync(p => p.Id == id);
        }


        public async Task DeletePlayerAsync(Guid playerid)
        {
            var player = await _playerRepository.Players.FirstOrDefaultAsync(p => p.Id == playerid);
            if (player == null) return;
            _playerRepository.Players.Remove(player);
            await _playerRepository.SaveChangesAsync();
        }

        public async Task<User?> GetUserWithPlayersAsync(Guid id)
        {
            return await _playerRepository.Users
                .Include(u => u.Player)
                    .ThenInclude(p => p.ChampionStats)
                .Include(u => u.Player)
                    .ThenInclude(p => p.RoleStats)
                .Include(u => u.Player)
                    .ThenInclude(p => p.MatchHistory)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> UserExistsAsync(Guid id)
        {
            return await _playerRepository.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<Player?> GetPlayerForUserByRiotIdentityAsync(Guid userId, string puuid, string riotDisplayName)
        {
            return await _playerRepository.Players
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId &&
                    ((!string.IsNullOrWhiteSpace(p.RiotPuuid) && p.RiotPuuid == puuid) ||
                     p.SummonerName.ToLower() == riotDisplayName.ToLower()));
        }

        public async Task<Player> AddSyncedPlayerAsync(Guid userId, Player player)
        {
            player.UserId = userId;
            if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();

            var entry = await _playerRepository.Players.AddAsync(player);
            await _playerRepository.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task ReplacePlayerPerformanceAsync(
            Guid playerId,
            List<PlayerChampionStat> championStats,
            List<PlayerRoleStat> roleStats,
            List<PlayerMatchHistory> matchHistory)
        {
            var existingChampionStats = await _playerRepository.PlayerChampionStats
                .Where(stat => stat.PlayerId == playerId)
                .ToListAsync();
            var existingRoleStats = await _playerRepository.PlayerRoleStats
                .Where(stat => stat.PlayerId == playerId)
                .ToListAsync();
            var existingMatchHistory = await _playerRepository.PlayerMatchHistory
                .Where(match => match.PlayerId == playerId)
                .ToListAsync();

            _playerRepository.PlayerChampionStats.RemoveRange(existingChampionStats);
            _playerRepository.PlayerRoleStats.RemoveRange(existingRoleStats);
            _playerRepository.PlayerMatchHistory.RemoveRange(existingMatchHistory);

            await _playerRepository.PlayerChampionStats.AddRangeAsync(championStats);
            await _playerRepository.PlayerRoleStats.AddRangeAsync(roleStats);
            await _playerRepository.PlayerMatchHistory.AddRangeAsync(matchHistory);
            await _playerRepository.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _playerRepository.SaveChangesAsync();
        }
        public async Task<Player> AddPlayerAsync(Guid UserId, Player player)
        {
            User? user = await _playerRepository.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null) throw new InvalidOperationException("User not found");
            player.UserId = UserId;
            if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();
            player.IsCaptain = false;
            if (player.TeamId.HasValue)
            {
                var teamExists = await _playerRepository.Teams.AnyAsync(t => t.Id == player.TeamId.Value);
                if (!teamExists)
                {
                    throw new InvalidOperationException("Team not found");
                }
            }

            var entry = await _playerRepository.Players.AddAsync(player);
            await _playerRepository.SaveChangesAsync();
            return entry.Entity;
        }
        public async Task<Player> IsCaptain(Guid id, bool i)
        {
            Player? player = await _playerRepository.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player != null)
            {
                player.IsCaptain = i;
                await _playerRepository.SaveChangesAsync();
            }
            return player!;
        }

        public async Task<Player> UploadProfileImage(Guid playerId, string profileImagePath)
        {
            var player = await _playerRepository.Players.FirstOrDefaultAsync(p => p.Id == playerId);
            if (player == null) return null!;
            player.ProfileImageUrl = profileImagePath;
            await _playerRepository.SaveChangesAsync();
            return player;
        }

        public async Task<Player> UpdateCompetitiveProfileAsync(Player player)
        {
            _playerRepository.Players.Update(player);
            await _playerRepository.SaveChangesAsync();
            return player;
        }

    }
}
