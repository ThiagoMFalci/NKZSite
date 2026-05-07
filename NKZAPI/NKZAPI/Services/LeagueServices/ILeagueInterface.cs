using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.LeagueServices
{
    public interface ILeagueInterface
    {
        Task<List<League>> GetAllLeaguesAsync();
        Task<League?> GetLeagueByIdAsync(Guid id);
        Task<Response<string>> AddLeagueAsync(League league);
        Task<Response<string>> UpdateLeagueAsync(League league);
        Task<Response<League>> UploadLeagueImageAsync(Guid leagueId, IFormFile image);
        Task DeleteLeagueAsync(League league);
        Task<Response<string>> DeleteLeagueByIdAsync(Guid leagueId);

        Task<Response<string>> AddTeamToLeagueAsync(Guid leagueId, Guid teamId);
        Task<Response<string>> ConfirmLeaguePaymentAsync(Guid paymentId);
        Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId);
        Task<Response<string>> RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId);
        Task<List<Team>> GetTeamsInLeagueAsync(Guid leagueId);
        Task<List<League>> GetLeaguesByTeamIdAsync(Guid teamId);
        Task<Response<string>> GeneratePlayoffAsync(Guid leagueId);
        Task<Response<string>> CompleteMatchAsync(Guid matchId, LeagueMatchResultDto result);
        Task<Response<string>> SubmitMatchReportAsync(Guid matchId, Guid reportedWinnerTeamId, IFormFile proofImage);
        Task<Response<string>> JoinRankingQueueAsync(Guid leagueId, Guid teamId);
        Task<Response<string>> LeaveRankingQueueAsync(Guid leagueId, Guid teamId);
        Task<Response<string>> ProposeMatchScheduleAsync(Guid matchId, LeagueMatchScheduleProposalDto proposal);
        Task<Response<string>> AcceptMatchScheduleAsync(Guid matchId);
        Task<Response<string>> RejectMatchScheduleAsync(Guid matchId);
    }
}
