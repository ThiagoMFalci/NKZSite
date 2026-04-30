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
            var players = await _playerRepository.Players
                .Include(p => p.ChampionStats)
                .Include(p => p.RoleStats)
                .Include(p => p.MatchHistory)
                .ToListAsync();
            await HydrateDiscordAsync(players);
            return players;
        }

        public async Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            var player = await _playerRepository.Players
                .Include(p => p.ChampionStats)
                .Include(p => p.RoleStats)
                .Include(p => p.MatchHistory)
                .FirstOrDefaultAsync(p => p.Id == id);
            await HydrateDiscordAsync(player == null ? new List<Player>() : new List<Player> { player });
            return player;
        }


        public async Task DeletePlayerAsync(Guid playerid)
        {
            var player = await _playerRepository.Players.FirstOrDefaultAsync(p => p.Id == playerid);
            if (player == null) return;
            var teamId = player.TeamId;
            _playerRepository.Players.Remove(player);
            await _playerRepository.SaveChangesAsync();
            await RecalculateTeamPointsAsync(teamId);
        }

        public async Task<User?> GetUserWithPlayersAsync(Guid id)
        {
            var user = await _playerRepository.Users
                .Include(u => u.Player)
                    .ThenInclude(p => p.ChampionStats)
                .Include(u => u.Player)
                    .ThenInclude(p => p.RoleStats)
                .Include(u => u.Player)
                    .ThenInclude(p => p.MatchHistory)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user?.Player != null)
            {
                foreach (var player in user.Player)
                {
                    player.DiscordUserId = user.DiscordUserId;
                    player.DiscordUsername = user.DiscordUsername;
                }
            }
            return user;
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
            await RecalculateTeamPointsAsync(player.TeamId);
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
            var affectedTeamIds = _playerRepository.ChangeTracker
                .Entries<Player>()
                .Where(entry => entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                .SelectMany(entry => new[]
                {
                    entry.Entity.TeamId,
                    entry.State == EntityState.Added
                        ? null
                        : entry.OriginalValues.GetValue<Guid?>(nameof(Player.TeamId))
                })
                .Where(teamId => teamId.HasValue)
                .Select(teamId => teamId!.Value)
                .Distinct()
                .ToList();

            await _playerRepository.SaveChangesAsync();

            foreach (var teamId in affectedTeamIds)
            {
                await RecalculateTeamPointsAsync(teamId);
            }
        }

        private async Task HydrateDiscordAsync(List<Player> players)
        {
            var userIds = players
                .Where(player => player.UserId.HasValue)
                .Select(player => player.UserId!.Value)
                .Distinct()
                .ToList();

            if (!userIds.Any()) return;

            var users = await _playerRepository.Users
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new { user.Id, user.DiscordUserId, user.DiscordUsername })
                .ToDictionaryAsync(user => user.Id);

            foreach (var player in players)
            {
                if (player.UserId.HasValue && users.TryGetValue(player.UserId.Value, out var user))
                {
                    player.DiscordUserId = user.DiscordUserId;
                    player.DiscordUsername = user.DiscordUsername;
                }
            }
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
            await RecalculateTeamPointsAsync(player.TeamId);
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
            var teamId = player.TeamId;
            _playerRepository.Players.Update(player);
            await _playerRepository.SaveChangesAsync();
            await RecalculateTeamPointsAsync(teamId);
            return player;
        }

        private async Task RecalculateTeamPointsAsync(Guid? teamId)
        {
            if (!teamId.HasValue) return;

            var team = await _playerRepository.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == teamId.Value);
            if (team == null) return;

            team.Points = team.Players?.Sum(CompetitivePoints.FromPlayer) ?? 0;
            await _playerRepository.SaveChangesAsync();
        }

    }
}
