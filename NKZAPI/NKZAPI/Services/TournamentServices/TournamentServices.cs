using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NKZAPI.Models;
using NKZAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace NKZAPI.Services.TournamentServices
{
    public class TournamentServices : ITournamentInterface
    {
        private readonly TournamentRepository _tournamentRepository;
        private readonly TeamRepository _teamRepository;

        public TournamentServices(TournamentRepository tournamentRepository, TeamRepository teamRepository)
        {
            _tournamentRepository = tournamentRepository;
            _teamRepository = teamRepository;
        }

        public async Task<Response<List<Tournament>>> GetAllTournamentsAsync()
        {
            var response = new Response<List<Tournament>>();
            try
            {
                var list = await _tournamentRepository.GetAllTournamentsAsync();
                response.Success = true;
                response.Data = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<Response<Tournament?>> GetTournamentByIdAsync(Guid id)
        {
            var response = new Response<Tournament?>();
            try
            {
                var tournament = await _tournamentRepository.GetTournamentByIdAsync(id);
                if (tournament == null)
                {
                    response.Success = false;
                    response.Message = "Tournament not found.";
                    response.Data = null;
                    return response;
                }
                response.Success = true;
                response.Data = tournament;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<Response<string>> AddTournamentAsync(Tournament tournament)
        {
            var response = new Response<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(tournament.Name))
                {
                    response.Success = false;
                    response.Message = "Tournament name is required.";
                    return response;
                }

                if (tournament.MaxTeams <= 0)
                {
                    response.Success = false;
                    response.Message = "MaxTeams must be greater than zero.";
                    return response;
                }

                if (tournament.Id == Guid.Empty) tournament.Id = Guid.NewGuid();

                var added = await _tournamentRepository.AddTournamentAsync(tournament);

                response.Success = true;
                response.Message = "Tournament created.";
                response.Data = added.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while adding the tournament: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<string>> UpdateTournamentAsync(Tournament tournament)
        {
            var response = new Response<string>();
            try
            {
                if (tournament.Id == Guid.Empty)
                {
                    response.Success = false;
                    response.Message = "Tournament Id is required.";
                    return response;
                }

                var existing = await _tournamentRepository.GetTournamentByIdAsync(tournament.Id);
                if (existing == null)
                {
                    response.Success = false;
                    response.Message = "Tournament not found.";
                    return response;
                }

                existing.Name = tournament.Name;
                existing.Prize = tournament.Prize;
                existing.EntryFee = tournament.EntryFee;
                existing.MaxTeams = tournament.MaxTeams;

                try
                {
                    await _tournamentRepository.UpdateTournamentAsync(existing);
                }
                catch (DbUpdateConcurrencyException)
                {
                    response.Success = false;
                    response.Message = "Concurrency conflict: the tournament was modified or deleted by another process.";
                    return response;
                }

                response.Success = true;
                response.Message = "Tournament updated.";
                response.Data = existing.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while updating the tournament: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<string>> DeleteTournamentAsync(Tournament tournament)
        {
            var response = new Response<string>();
            try
            {
                if (tournament.Id == Guid.Empty)
                {
                    response.Success = false;
                    response.Message = "Tournament Id is required.";
                    return response;
                }

                // Busca a entidade completa para evitar problemas com entidade desconectada
                var existing = await _tournamentRepository.GetTournamentByIdAsync(tournament.Id);
                if (existing == null)
                {
                    response.Success = false;
                    response.Message = "Tournament not found.";
                    return response;
                }

                await _tournamentRepository.DeleteTournamentAsync(existing);
                response.Success = true;
                response.Message = "Tournament deleted.";
                response.Data = existing.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while deleting the tournament: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<string>> AddTeamToTournamentAsync(Guid tournamentId, Guid teamId)
        {
            var response = new Response<string>();
            try
            {
                var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);
                if (tournament == null)
                {
                    response.Success = false;
                    response.Message = "Tournament not found.";
                    return response;
                }

                var team = await _teamRepository.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    response.Success = false;
                    response.Message = "Team not found.";
                    return response;
                }

                if (tournament.Teams.Any(t => t.Id == teamId))
                {
                    response.Success = false;
                    response.Message = "Team already in tournament.";
                    return response;
                }

                if (tournament.Teams.Count >= tournament.MaxTeams)
                {
                    response.Success = false;
                    response.Message = "Tournament has reached its maximum number of teams.";
                    return response;
                }

                await _tournamentRepository.AddTeamToTournamentAsync(tournamentId, team);
                response.Success = true;
                response.Message = "Team added to tournament.";
                response.Data = teamId.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while adding team to tournament: {ex.Message}";
            }
            return response;
        }

        // Helper to keep calling service-style repository method with correct naming
        private async Task<Team?> _tournament_repository_GetTeam(Guid teamId)
        {
            return await _teamRepository.GetTeamByIdAsync(teamId);
        }

        public async Task<Response<string>> RemoveTeamFromTournamentAsync(Guid tournamentId, Guid teamId)
        {
            var response = new Response<string>();
            try
            {
                var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);
                if (tournament == null)
                {
                    response.Success = false;
                    response.Message = "Tournament not found.";
                    return response;
                }

                if (!tournament.Teams.Any(t => t.Id == teamId))
                {
                    response.Success = false;
                    response.Message = "Team is not part of this tournament.";
                    return response;
                }

                await _tournamentRepository.RemoveTeamFromTournamentAsync(tournamentId, teamId);
                response.Success = true;
                response.Message = "Team removed from tournament.";
                response.Data = teamId.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while removing team from tournament: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<List<Team>>> GetTeamsInTournamentAsync(Guid tournamentId)
        {
            var response = new Response<List<Team>>();
            try
            {
                var teams = await _tournamentRepository.GetTeamsInTournamentAsync(tournamentId);
                response.Success = true;
                response.Data = teams;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<Response<List<Tournament>>> GetTournamentsByTeamIdAsync(Guid teamId)
        {
            var response = new Response<List<Tournament>>();
            try
            {
                var list = await _tournamentRepository.GetTournamentsByTeamIdAsync(teamId);
                response.Success = true;
                response.Data = list;
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
