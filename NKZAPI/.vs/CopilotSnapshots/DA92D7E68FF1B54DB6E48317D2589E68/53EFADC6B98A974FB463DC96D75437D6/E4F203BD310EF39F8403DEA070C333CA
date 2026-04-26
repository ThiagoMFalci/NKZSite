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
        Task<Response<string>> DeleteTeamAsync(Team team);

        // Invitation/request flow
        Task<Response<Invitation>> CreateInvitationAsync(Invitation invitation);
        Task<Response<string>> RespondToInvitationAsync(Guid invitationId, bool accept);
        Task<List<Invitation>> GetInvitationsForPlayerAsync(Guid playerId);
        Task<List<Invitation>> GetInvitationsForTeamAsync(Guid teamId);

        Task<Response<Player>> AddPlayerToTeamAsync(Guid teamId, Guid playerId);

        Task<Response<string>> ExpelPlayerAsync(Guid teamId, Guid playerId);

        // legacy direct remove
        Task<Response<string>> RemovePlayerFromTeamAsync(Guid teamId, Guid playerId);

        Task<Response<string>> AssignCaptainAsync(Guid teamId, Guid playerId, bool i);
    }
}
