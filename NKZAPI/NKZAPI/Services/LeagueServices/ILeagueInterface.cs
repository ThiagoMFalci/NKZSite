
using NKZAPI.Models;

namespace NKZAPI.Services.LeagueServices
{
    public interface ILeagueInterface
    {
        Task<List<League>> GetAllLeaguesAsync();
        Task<League?> GetLeagueByIdAsync(Guid id);
        Task<Response<string>> AddLeagueAsync(League league);
        Task<Response<string>> UpdateLeagueAsync(League league);
        Task DeleteLeagueAsync(League league);

        Task<Response<string>> AddTeamToLeagueAsync(Guid leagueId, Guid teamId);
        Task<Response<string>> RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId);
        Task<List<Team>> GetTeamsInLeagueAsync(Guid leagueId);
        Task<List<League>> GetLeaguesByTeamIdAsync(Guid teamId);
    }
}