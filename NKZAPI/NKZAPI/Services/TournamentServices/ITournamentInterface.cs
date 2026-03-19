using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NKZAPI.Models;

namespace NKZAPI.Services.TournamentServices
{
    public interface ITournamentInterface
    {
        Task<Response<List<Tournament>>> GetAllTournamentsAsync();
        Task<Response<Tournament?>> GetTournamentByIdAsync(Guid id);
        Task<Response<string>> AddTournamentAsync(Tournament tournament);
        Task<Response<string>> UpdateTournamentAsync(Tournament tournament);
        Task<Response<string>> DeleteTournamentAsync(Tournament tournament);
        Task<Response<string>> AddTeamToTournamentAsync(Guid tournamentId, Guid teamId);
        Task<Response<string>> RemoveTeamFromTournamentAsync(Guid tournamentId, Guid teamId);
        Task<Response<List<Team>>> GetTeamsInTournamentAsync(Guid tournamentId);
        Task<Response<List<Tournament>>> GetTournamentsByTeamIdAsync(Guid teamId);
    }
}
