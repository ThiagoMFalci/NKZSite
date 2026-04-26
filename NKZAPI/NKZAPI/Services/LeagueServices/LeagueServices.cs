
using Microsoft.EntityFrameworkCore;
using NKZAPI.Models;
using NKZAPI.Repositories;
using System.Security.Claims;

namespace NKZAPI.Services.LeagueServices
{
    public class LeagueServices : ILeagueInterface
    {
        private readonly LeagueRepository _leagueRepository;
        private readonly TeamRepository _teamRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeagueServices(LeagueRepository leagueRepository, TeamRepository teamRepository, IHttpContextAccessor httpContextAccessor)
        {
            _leagueRepository = leagueRepository;
            _teamRepository = teamRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<League>> GetAllLeaguesAsync()
        {
            return await _leagueRepository.GetAllLeaguesAsync();
        }

        public async Task<League?> GetLeagueByIdAsync(Guid id)
        {
            return await _leagueRepository.GetLeagueByIdAsync(id);
        }

        public async Task<Response<string>> AddLeagueAsync(League league)
        {
            var response = new Response<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(league.Name))
                {
                    response.Success = false;
                    response.Message = "League name is required.";
                    return response;
                }

                if (league.MaxTeams <= 0)
                {
                    response.Success = false;
                    response.Message = "MaxTeams must be greater than zero.";
                    return response;
                }

                if (league.Id == Guid.Empty) league.Id = Guid.NewGuid();

                var added = await _leagueRepository.AddLeagueAsync(league);

                response.Success = true;
                response.Message = "League created.";
                response.Data = added.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while adding the league: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<string>> UpdateLeagueAsync(League league)
        {
            var response = new Response<string>();
            try
            {
                if (league.Id == Guid.Empty)
                {
                    response.Success = false;
                    response.Message = "League Id is required.";
                    return response;
                }

                var existing = await _leagueRepository.GetLeagueByIdAsync(league.Id);
                if (existing == null)
                {
                    response.Success = false;
                    response.Message = "League not found.";
                    return response;
                }

                existing.Name = league.Name;
                existing.Award = league.Award;
                existing.EntryFee = league.EntryFee;
                existing.MaxTeams = league.MaxTeams;

                try
                {
                    await _leagueRepository.UpdateLeagueAsync(existing);
                }
                catch (DbUpdateConcurrencyException)
                {
                    response.Success = false;
                    response.Message = "Concurrency conflict: the league was modified or deleted by another process.";
                    return response;
                }

                response.Success = true;
                response.Message = "League updated.";
                response.Data = existing.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while updating the league: {ex.Message}";
            }
            return response;
        }

        public async Task DeleteLeagueAsync(League league)
        {
            await _leagueRepository.DeleteLeagueAsync(league);
        }

        public async Task<Response<string>> AddTeamToLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = new Response<string>();
            try
            {
                var league = await _leagueRepository.GetLeagueByIdAsync(leagueId);
                if (league == null)
                {
                    response.Success = false;
                    response.Message = "League not found.";
                    return response;
                }

                var team = await _teamRepository.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    response.Success = false;
                    response.Message = "Team not found.";
                    return response;
                }

                if (!CanManageTeam(team, out var authMessage))
                {
                    response.Success = false;
                    response.Message = authMessage;
                    return response;
                }

                if (league.Teams.Any(t => t.Id == teamId))
                {
                    response.Success = false;
                    response.Message = "Team already in league.";
                    return response;
                }

                if (league.Teams.Count >= league.MaxTeams)
                {
                    response.Success = false;
                    response.Message = "League has reached its maximum number of teams.";
                    return response;
                }

                await _leagueRepository.AddTeamToLeagueAsync(leagueId, team);
                response.Success = true;
                response.Message = "Team added to league.";
                response.Data = teamId.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while adding team to league: {ex.Message}";
            }
            return response;
        }

        public async Task<Response<string>> RemoveTeamFromLeagueAsync(Guid leagueId, Guid teamId)
        {
            var response = new Response<string>();
            try
            {
                var league = await _leagueRepository.GetLeagueByIdAsync(leagueId);
                if (league == null)
                {
                    response.Success = false;
                    response.Message = "League not found.";
                    return response;
                }

                var team = await _teamRepository.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    response.Success = false;
                    response.Message = "Team not found.";
                    return response;
                }

                if (!CanManageTeam(team, out var authMessage))
                {
                    response.Success = false;
                    response.Message = authMessage;
                    return response;
                }

                if (!league.Teams.Any(t => t.Id == teamId))
                {
                    response.Success = false;
                    response.Message = "Team is not part of this league.";
                    return response;
                }

                await _leagueRepository.RemoveTeamFromLeagueAsync(leagueId, teamId);
                response.Success = true;
                response.Message = "Team removed from league.";
                response.Data = teamId.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred while removing team from league: {ex.Message}";
            }
            return response;
        }

        public async Task<List<Team>> GetTeamsInLeagueAsync(Guid leagueId)
        {
            return await _leagueRepository.GetTeamsInLeagueAsync(leagueId);
        }

        public async Task<List<League>> GetLeaguesByTeamIdAsync(Guid teamId)
        {
            return await _leagueRepository.GetLeaguesByTeamIdAsync(teamId);
        }

        private bool CanManageTeam(Team team, out string message)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var callerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;

            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
            {
                message = "Unauthorized";
                return false;
            }

            var isAdmin = user?.IsInRole("Admin") == true || user?.Claims.Any(c => c.Type == "role" && c.Value == "Admin") == true;
            var isOwner = team.OwnerId == callerId;
            var isCaptain = team.Players?.Any(player => player.UserId == callerId && player.IsCaptain) == true;

            if (!isAdmin && !isOwner && !isCaptain)
            {
                message = "Forbidden";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
