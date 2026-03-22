using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Repositories;
using NKZAPI.Services.PassService;
using NKZAPI.Services.PlayerServices;
using NKZAPI.Services.RiotService;
using NKZAPI.Services.TeamServices;
using NKZAPI.Services.TournamentServices;

namespace NKZAPI.Services.UserServices
{
    public class UserServices : IUserInterface
    {
        private readonly NKZAPIContext _context;
        private readonly UserRepository _userRepository;
        private readonly IPasswordInterface _passInterface;
        private readonly IPlayerInterface _playerInterface;
        private readonly ITournamentInterface _TournamentInterface;
        private readonly ITeamInterface _TeamInterface;
        private readonly IRiotService _riotService;

        public UserServices(UserRepository userRepository, NKZAPIContext NKZAPI, IPasswordInterface passInterface, IRiotService riotService, IPlayerInterface playerInterface, ITournamentInterface tournamentInterface, ITeamInterface teamInterface)
        {
            _userRepository = userRepository;
            _context = NKZAPI;
            _passInterface = passInterface;
            _riotService = riotService;
            _playerInterface = playerInterface;
            _TeamInterface = teamInterface;
            _TournamentInterface = tournamentInterface;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var user2 = await _userRepository.GetAllUsersAsync();
            user2.ForEach(u =>
            {
                u.PasswordSalt = null;
                u.PasswordHash = null;
            });
            return user2;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user2 = await _userRepository.GetUserByIdAsync(id);
            user2.PasswordSalt = null;
            user2.PasswordHash = null;
            return user2;
        }

        public async Task<Response<User>> AddUserAsync(User user)
        {
            var response = new Response<User>();
            try
            {
                response.Data = await _userRepository.AddUserAsync(user);
                response.Message = "User added successfully";
                response.Success = true;
                return response;

            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = $"Error adding user: {ex.Message}";
                response.Success = false;
                return response;
            }

        }

        public async Task<Response<string>> UpdateUserAsync(UserDto user, Guid id)
        {
            var response = new Response<string>();

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
            {
                response.Data = null;
                response.Message = "User not found";
                response.Success = false;
                return response;
            }

            _passInterface.CreatePassHash(user.PasswordHash, out byte[] passwordHash, out byte[] passwordSalt);
            var dto = await _userRepository.GetUserByIdAsync(id);

            existingUser.Email = user.Email;
            existingUser.PasswordHash = passwordHash;
            existingUser.PasswordSalt = passwordSalt;
            existingUser.Role = user.Role;

            await _userRepository.UpdateUserAsync(existingUser);

            response.Data = id.ToString();
            response.Message = "User updated successfully";
            response.Success = true;
            return response;
        }
        public async Task<Response<User>> DeleteUserAsync(Guid id)
        {
            var response = new Response<User>();
            try
            {
                var existingUser = await _context.Users
                    .Include(u => u.Player)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (existingUser == null)
                {
                    response.Data = null;
                    response.Message = "User not found";
                    response.Success = false;
                    return response;
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (existingUser.Player != null && existingUser.Player.Any())
                {
                    _context.Players.RemoveRange(existingUser.Player);
                }

                var teams = await _context.Teams
                    .Include(t => t.Players)
                    .Where(t => t.OwnerId == id)
                    .ToListAsync();

                foreach (var team in teams)
                {
                    if (team.Players != null)
                    {
                        foreach (var p in team.Players)
                        {
                            // if the player belongs to another user, just dissociate from team
                            if (p.UserId != id)
                            {
                                p.TeamId = null;
                            }
                        }
                    }
                }
                if (teams.Any()) _context.Teams.RemoveRange(teams);

                // Delete tournaments created by this user
                var tournaments = await _context.Set<Tournament>()
                    .Where(t => t.OwnerId == id)
                    .ToListAsync();
                if (tournaments.Any()) _context.Set<Tournament>().RemoveRange(tournaments);

                // Finally remove the user
                _context.Users.Remove(existingUser);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Data = existingUser;
                response.Message = "User deleted successfully";
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = $"Error deleting user: {ex.Message}";
                response.Success = false;
                return response;
            }
        }

        public async Task<Response<string>> UpdatePlayerFromRiotAsync(Guid userId, string summonerName, string region = "br1")
        {
            var response = new Response<string>();
            try
            {
                var user = await _context.Users
                    .Include(u => u.Player)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    return response;
                }

                var player = user.Player?.FirstOrDefault(p => p.SummonerName.Equals(summonerName, StringComparison.OrdinalIgnoreCase));

                if (player == null)
                {
                    player = new Player
                    {
                        Id = Guid.NewGuid(),
                        SummonerName = summonerName,
                        IsCaptain = false,
                        IsActive = true,
                        LastStatsUpdate = DateTime.UtcNow
                    };
                    if (user.Player == null) user.Player = new List<Player>();
                    user.Player.Add(player);
                }

                var summoner = await _riotService.GetSummonerByNameAsync(region, summonerName);
                if (summoner == null)
                {
                    response.Success = false;
                    response.Message = "Summoner not found on Riot.";
                    return response;
                }

                player.RiotPuuid = summoner.Puuid;
                player.SummonerLevel = (int)summoner.SummonerLevel;
                player.SummonerName = summoner.Name;

                // ranked
                var leagueEntry = await _riotService.GetSoloQueueEntryAsync(region, summoner.Id);
                if (leagueEntry != null)
                {
                    player.SoloQueueTier = leagueEntry.Tier ?? "UNRANKED";
                    player.SoloQueueRank = leagueEntry.Rank ?? "";
                    player.SoloQueueLP = leagueEntry.LeaguePoints;
                    player.Wins = leagueEntry.Wins;
                    player.Losses = leagueEntry.Losses;
                    player.TotalMatches = leagueEntry.Wins + leagueEntry.Losses;
                    player.IsVerified = true;
                }

                player.LastStatsUpdate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Player updated from Riot.";
                response.Data = player.Id.ToString();
            }
            catch (HttpRequestException ex)
            {
                response.Success = false;
                response.Message = $"Riot API error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

    }
}
