using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.PlayerServices
{
    public interface IPlayerInterface
    {
        Task<Response<Player>> UpdatePlayerFromRiotAsync(Guid userId, string summonerName, string region = "br1");
        Task<Response<Player>> AddPlayerAsync(Guid UserId, Player player);
        Task<Response<Player>> DeletePlayerAsync(Guid playerid);
        Task<Response<Player>> GetPlayerByIdAsync(Guid id);
        Task<Response<List<Player>>> GetAllPlayersAsync();
        Task<Response<Player>> GetPlayerByUserIdAsync(Guid userId);
        Task<Response<Player>> UploadProfileImage(Guid playerId, IFormFile profileImageUrl);
        Task<Response<Player>> UpdateCompetitiveProfileAsync(Guid userId, PlayerCompetitiveProfileDto profile);
    }
}
