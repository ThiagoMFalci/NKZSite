using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.TeamServices
{
    public interface ITeamInterface
    {
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team?> GetTeamByIdAsync(Guid id);
        Task<Response<string>> AddTeamAsync(TeamDto team, Guid id);
        Task<Response<string>> UpdateTeamAsync(TeamDto team);
        Task DeleteTeamAsync(Team team);

        Task<Player> AddPlayerToTeamAsync(Guid teamId, Player player);
        Task RemovePlayerFromTeamAsync(Guid teamId, Guid playerId);
    }
}
