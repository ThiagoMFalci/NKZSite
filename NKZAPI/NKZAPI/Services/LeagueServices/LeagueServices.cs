
using Microsoft.EntityFrameworkCore;
using NKZAPI.Dtos;
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
        private readonly IWebHostEnvironment _env;

        public LeagueServices(LeagueRepository leagueRepository, TeamRepository teamRepository, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            _leagueRepository = leagueRepository;
            _teamRepository = teamRepository;
            _httpContextAccessor = httpContextAccessor;
            _env = env;
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

                league.MaxTeams = 16;
                if (string.IsNullOrWhiteSpace(league.Modality))
                {
                    league.Modality = "Chaveamento";
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
                existing.ImageUrl = league.ImageUrl ?? existing.ImageUrl;
                existing.MaxTeams = league.MaxTeams;
                existing.MinimumElo = league.MinimumElo;
                existing.MaximumElo = league.MaximumElo;
                existing.StartDate = league.StartDate;
                existing.EndDate = league.EndDate;
                existing.Modality = league.Modality;

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

        public async Task<Response<League>> UploadLeagueImageAsync(Guid leagueId, IFormFile image)
        {
            var response = new Response<League>();
            try
            {
                var league = await _leagueRepository.GetLeagueByIdAsync(leagueId);
                if (league == null)
                {
                    response.Success = false;
                    response.Message = "League not found.";
                    return response;
                }

                if (image == null || image.Length == 0)
                {
                    response.Success = false;
                    response.Message = "No file uploaded.";
                    return response;
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var maxFileSize = 5 * 1024 * 1024;
                var fileExt = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!image.ContentType.StartsWith("image/") || !allowedExtensions.Contains(fileExt))
                {
                    response.Success = false;
                    response.Message = "Invalid file type. Only image files are allowed.";
                    return response;
                }

                if (image.Length > maxFileSize)
                {
                    response.Success = false;
                    response.Message = "File too large. Maximum allowed size is 5 MB.";
                    return response;
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "leagues");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{leagueId}{fileExt}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("images", "leagues", fileName).Replace("\\", "/");
                var updated = await _leagueRepository.UploadLeagueImageAsync(leagueId, relativePath);

                response.Success = true;
                response.Message = "League image uploaded successfully.";
                response.Data = updated;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
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

                if (league.Matches.Any())
                {
                    response.Success = false;
                    response.Message = "O chaveamento desta liga ja foi gerado.";
                    return response;
                }

                if (league.StartDate.HasValue && DateTime.UtcNow >= league.StartDate.Value.ToUniversalTime())
                {
                    response.Success = false;
                    response.Message = "As inscricoes desta liga ja foram encerradas.";
                    return response;
                }

                if (league.Teams.Count >= league.MaxTeams)
                {
                    response.Success = false;
                    response.Message = "League has reached its maximum number of teams.";
                    return response;
                }

                await _leagueRepository.AddTeamToLeagueAsync(leagueId, team);
                var updatedLeague = await _leagueRepository.GetLeagueByIdAsync(leagueId);
                if (updatedLeague != null && updatedLeague.Teams.Count == 16 && !updatedLeague.Matches.Any())
                {
                    await GeneratePlayoffAsync(leagueId);
                }
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

                if (league.Matches.Any())
                {
                    response.Success = false;
                    response.Message = "Nao e possivel remover times depois que o chaveamento foi gerado.";
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

        public async Task<Response<string>> GeneratePlayoffAsync(Guid leagueId)
        {
            var response = new Response<string>();
            var league = await _leagueRepository.GetLeagueByIdAsync(leagueId);
            if (league == null)
            {
                response.Success = false;
                response.Message = "League not found.";
                return response;
            }

            if (league.Teams.Count != 16)
            {
                response.Success = false;
                response.Message = "A liga precisa ter exatamente 16 times para gerar o chaveamento.";
                return response;
            }

            var seededTeams = league.Teams
                .OrderByDescending(GetTeamSeedScore)
                .ThenBy(team => team.Name)
                .ToList();
            var seedOrder = new[] { 0, 15, 7, 8, 4, 11, 3, 12, 2, 13, 5, 10, 6, 9, 1, 14 };
            var ordered = seedOrder.Select(index => seededTeams[index]).ToList();
            var startDate = league.StartDate ?? DateTime.UtcNow;
            var matches = new List<LeagueMatch>();

            for (var index = 0; index < 8; index++)
            {
                matches.Add(CreateMatch(leagueId, "Upper", "U-R16", "Oitavas Upper", 1, index + 1, ordered[index * 2].Id, ordered[index * 2 + 1].Id, 1, startDate));
            }

            AddEmptyRound(matches, leagueId, "Upper", "U-QF", "Quartas Upper", 2, 4, 1, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-R1", "Rodada 1 Lower", 2, 4, 1, startDate);
            AddEmptyRound(matches, leagueId, "Upper", "U-SF", "Semifinal Upper", 3, 2, 1, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-R2", "Rodada 2 Lower", 3, 4, 1, startDate);
            AddEmptyRound(matches, leagueId, "Upper", "U-F", "Final Upper", 4, 1, 3, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-R3", "Rodada 3 Lower", 4, 2, 1, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-R4", "Rodada 4 Lower", 5, 2, 1, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-R5", "Rodada 5 Lower", 6, 1, 3, startDate);
            AddEmptyRound(matches, leagueId, "Lower", "L-F", "Final Lower", 7, 1, 3, startDate);
            AddEmptyRound(matches, leagueId, "GrandFinal", "GF", "Grande Final", 8, 1, 5, startDate);

            var standings = league.Teams.Select(team => new LeagueStanding
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                TeamId = team.Id
            }).ToList();

            await _leagueRepository.ReplacePlayoffAsync(leagueId, matches, standings);

            response.Success = true;
            response.Message = "Chaveamento gerado.";
            response.Data = leagueId.ToString();
            return response;
        }

        public async Task<Response<string>> CompleteMatchAsync(Guid matchId, LeagueMatchResultDto result)
        {
            var response = new Response<string>();
            var match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            if (match.TeamAId == null || match.TeamBId == null)
            {
                response.Success = false;
                response.Message = "A partida ainda nao possui dois times definidos.";
                return response;
            }

            if (match.Status == "Completed")
            {
                response.Success = false;
                response.Message = "Esta partida ja foi finalizada.";
                return response;
            }

            if (result.WinnerTeamId != match.TeamAId && result.WinnerTeamId != match.TeamBId)
            {
                response.Success = false;
                response.Message = "Vencedor invalido para esta partida.";
                return response;
            }

            if (result.TeamAScore < 0 || result.TeamBScore < 0 || result.TeamAScore == result.TeamBScore)
            {
                response.Success = false;
                response.Message = "Informe um placar valido, sem empate.";
                return response;
            }

            await CompleteMatchCoreAsync(match, result.WinnerTeamId, result.TeamAScore, result.TeamBScore);

            response.Success = true;
            response.Message = "Partida finalizada.";
            response.Data = match.Id.ToString();
            return response;
        }

        public async Task<Response<string>> SubmitMatchReportAsync(Guid matchId, Guid reportedWinnerTeamId, IFormFile proofImage)
        {
            var response = new Response<string>();
            var match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            if (match.TeamAId == null || match.TeamBId == null)
            {
                response.Success = false;
                response.Message = "A partida ainda nao possui dois times definidos.";
                return response;
            }

            if (match.Status == "Completed")
            {
                response.Success = false;
                response.Message = "Esta partida ja foi finalizada.";
                return response;
            }

            if (reportedWinnerTeamId != match.TeamAId && reportedWinnerTeamId != match.TeamBId)
            {
                response.Success = false;
                response.Message = "Vencedor invalido para esta partida.";
                return response;
            }

            if (proofImage == null || proofImage.Length == 0)
            {
                response.Success = false;
                response.Message = "Envie um print para provar o resultado.";
                return response;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            var callerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
            {
                response.Success = false;
                response.Message = "Unauthorized";
                return response;
            }

            var teamA = await _teamRepository.GetTeamByIdAsync(match.TeamAId.Value);
            var teamB = await _teamRepository.GetTeamByIdAsync(match.TeamBId.Value);
            var callerTeam = teamA != null && CanManageTeamForUser(teamA, callerId, user) ? teamA :
                teamB != null && CanManageTeamForUser(teamB, callerId, user) ? teamB : null;

            if (callerTeam == null)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            string proofPath;
            try
            {
                proofPath = await SaveProofImageAsync(matchId, callerTeam.Id, proofImage);
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
            var report = await _leagueRepository.GetMatchReportAsync(matchId, callerTeam.Id);
            if (report == null)
            {
                report = new LeagueMatchReport
                {
                    Id = Guid.NewGuid(),
                    LeagueMatchId = matchId,
                    TeamId = callerTeam.Id,
                    ReportedWinnerTeamId = reportedWinnerTeamId,
                    SubmittedByUserId = callerId,
                    ProofImageUrl = proofPath,
                    UpdatedAt = DateTime.UtcNow
                };
                await _leagueRepository.AddMatchReportAsync(report);
            }
            else
            {
                report.ReportedWinnerTeamId = reportedWinnerTeamId;
                report.SubmittedByUserId = callerId;
                report.ProofImageUrl = proofPath;
                report.UpdatedAt = DateTime.UtcNow;
                await _leagueRepository.SaveChangesAsync();
            }

            match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            var teamReports = match.Reports
                .Where(item => item.TeamId == match.TeamAId || item.TeamId == match.TeamBId)
                .GroupBy(item => item.TeamId)
                .Select(group => group.OrderByDescending(item => item.UpdatedAt).First())
                .ToList();

            if (teamReports.Count == 2 && teamReports.Select(item => item.ReportedWinnerTeamId).Distinct().Count() == 1)
            {
                var winnerId = teamReports[0].ReportedWinnerTeamId;
                await CompleteMatchCoreAsync(match, winnerId, winnerId == match.TeamAId ? 1 : 0, winnerId == match.TeamBId ? 1 : 0);
                response.Message = "Resultado confirmado pelos dois times. Partida finalizada.";
            }
            else
            {
                match.Status = teamReports.Count == 2 ? "Disputed" : "Reported";
                await _leagueRepository.SaveChangesAsync();
                response.Message = teamReports.Count == 2
                    ? "Os times escolheram vencedores diferentes. Um administrador precisa decidir."
                    : "Resultado enviado. Aguardando confirmacao do outro time.";
            }

            response.Success = true;
            response.Data = match.Id.ToString();
            return response;
        }

        public async Task<Response<string>> ProposeMatchScheduleAsync(Guid matchId, LeagueMatchScheduleProposalDto proposal)
        {
            var response = new Response<string>();
            var match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            if (match.TeamAId == null || match.TeamBId == null)
            {
                response.Success = false;
                response.Message = "A partida ainda nao possui dois times definidos.";
                return response;
            }

            if (match.Status == "Completed")
            {
                response.Success = false;
                response.Message = "Esta partida ja foi finalizada.";
                return response;
            }

            if (proposal.ProposedScheduledAt == default)
            {
                response.Success = false;
                response.Message = "Informe um horario valido.";
                return response;
            }

            var callerTeam = await GetCallerMatchTeamAsync(match);
            if (callerTeam == null)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            match.ProposedScheduledAt = proposal.ProposedScheduledAt.ToUniversalTime();
            match.ProposedByTeamId = callerTeam.Id;
            match.ScheduleStatus = "Pending";
            await _leagueRepository.SaveChangesAsync();

            response.Success = true;
            response.Message = "Horario sugerido. Aguardando confirmacao do outro time.";
            response.Data = match.Id.ToString();
            return response;
        }

        public async Task<Response<string>> AcceptMatchScheduleAsync(Guid matchId)
        {
            var response = new Response<string>();
            var match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            if (match.ProposedScheduledAt == null || match.ProposedByTeamId == null || match.ScheduleStatus != "Pending")
            {
                response.Success = false;
                response.Message = "Nao existe horario pendente para confirmar.";
                return response;
            }

            if (match.Status == "Completed")
            {
                response.Success = false;
                response.Message = "Esta partida ja foi finalizada.";
                return response;
            }

            var callerTeam = await GetCallerMatchTeamAsync(match);
            if (callerTeam == null)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            if (callerTeam.Id == match.ProposedByTeamId)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            match.ScheduledAt = match.ProposedScheduledAt;
            match.ScheduleStatus = "Confirmed";
            await _leagueRepository.SaveChangesAsync();

            response.Success = true;
            response.Message = "Horario confirmado.";
            response.Data = match.Id.ToString();
            return response;
        }

        public async Task<Response<string>> RejectMatchScheduleAsync(Guid matchId)
        {
            var response = new Response<string>();
            var match = await _leagueRepository.GetLeagueMatchByIdAsync(matchId);
            if (match == null)
            {
                response.Success = false;
                response.Message = "Match not found.";
                return response;
            }

            if (match.ProposedByTeamId == null || match.ScheduleStatus != "Pending")
            {
                response.Success = false;
                response.Message = "Nao existe horario pendente para recusar.";
                return response;
            }

            if (match.Status == "Completed")
            {
                response.Success = false;
                response.Message = "Esta partida ja foi finalizada.";
                return response;
            }

            var callerTeam = await GetCallerMatchTeamAsync(match);
            if (callerTeam == null)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            if (callerTeam.Id == match.ProposedByTeamId)
            {
                response.Success = false;
                response.Message = "Forbidden";
                return response;
            }

            match.ScheduleStatus = "Rejected";
            await _leagueRepository.SaveChangesAsync();

            response.Success = true;
            response.Message = "Horario recusado. Sugira outro horario se quiser.";
            response.Data = match.Id.ToString();
            return response;
        }

        private async Task CompleteMatchCoreAsync(LeagueMatch match, Guid winnerId, int teamAScore, int teamBScore)
        {
            var loserId = winnerId == match.TeamAId ? match.TeamBId!.Value : match.TeamAId!.Value;

            match.WinnerTeamId = winnerId;
            match.LoserTeamId = loserId;
            match.TeamAScore = teamAScore;
            match.TeamBScore = teamBScore;
            match.Status = "Completed";
            match.CompletedAt = DateTime.UtcNow;
            await _leagueRepository.UpdateLeagueMatchAsync(match);

            await UpdateStandingAsync(match.LeagueId, winnerId, true, teamAScore, teamBScore, match.TeamAId == winnerId);
            await UpdateStandingAsync(match.LeagueId, loserId, false, teamAScore, teamBScore, match.TeamAId == loserId);
            await AdvanceBracketAsync(match, winnerId, loserId);
        }

        private static LeagueMatch CreateMatch(Guid leagueId, string bracket, string roundKey, string roundName, int week, int matchNumber, Guid? teamAId, Guid? teamBId, int bestOf, DateTime startDate)
        {
            return new LeagueMatch
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                Bracket = bracket,
                RoundKey = roundKey,
                RoundName = roundName,
                WeekNumber = week,
                MatchNumber = matchNumber,
                BestOf = bestOf,
                TeamAId = teamAId,
                TeamBId = teamBId,
                ScheduledAt = startDate.Date.AddDays((week - 1) * 7)
            };
        }

        private static void AddEmptyRound(List<LeagueMatch> matches, Guid leagueId, string bracket, string roundKey, string roundName, int week, int count, int bestOf, DateTime startDate)
        {
            for (var index = 1; index <= count; index++)
            {
                matches.Add(CreateMatch(leagueId, bracket, roundKey, roundName, week, index, null, null, bestOf, startDate));
            }
        }

        private static double GetTeamSeedScore(Team team)
        {
            var players = team.Players ?? new List<Player>();
            return players.Sum(GetPlayerSeedScore);
        }

        private static double GetPlayerSeedScore(Player player)
        {
            var tierScore = GetTierScore(player.SoloQueueTier);
            var rankScore = GetRankScore(player.SoloQueueRank);
            var winRate = player.Wins + player.Losses > 0
                ? (double)player.Wins / (player.Wins + player.Losses)
                : 0;

            return tierScore * 1000 + rankScore * 100 + Math.Max(0, player.SoloQueueLP) + winRate;
        }

        private static int GetTierScore(string? tier)
        {
            return (tier ?? "").Trim().ToUpperInvariant() switch
            {
                "IRON" => 1,
                "BRONZE" => 2,
                "SILVER" => 3,
                "GOLD" => 4,
                "PLATINUM" => 5,
                "EMERALD" => 6,
                "DIAMOND" => 7,
                "MASTER" => 8,
                "GRANDMASTER" => 9,
                "CHALLENGER" => 10,
                _ => 0
            };
        }

        private static int GetRankScore(string? rank)
        {
            return (rank ?? "").Trim().ToUpperInvariant() switch
            {
                "IV" => 1,
                "III" => 2,
                "II" => 3,
                "I" => 4,
                _ => 0
            };
        }

        private async Task UpdateStandingAsync(Guid leagueId, Guid teamId, bool won, int scoreA, int scoreB, bool isTeamA)
        {
            var standing = await _leagueRepository.GetStandingAsync(leagueId, teamId);
            if (standing == null) return;

            var ownScore = isTeamA ? scoreA : scoreB;
            var opponentScore = isTeamA ? scoreB : scoreA;
            if (won) standing.Wins += 1;
            else standing.Losses += 1;
            standing.MapsPlayed += Math.Max(1, ownScore + opponentScore);
            standing.MapDiff += ownScore - opponentScore;
            await _leagueRepository.SaveChangesAsync();
        }

        private async Task AdvanceBracketAsync(LeagueMatch match, Guid winnerId, Guid loserId)
        {
            var matches = await _leagueRepository.GetLeagueMatchesAsync(match.LeagueId);
            LeagueMatch? next = null;

            void Place(LeagueMatch? target, Guid teamId, bool slotA)
            {
                if (target == null) return;
                if (slotA) target.TeamAId = teamId;
                else target.TeamBId = teamId;
                target.Status = target.TeamAId != null && target.TeamBId != null ? "Ready" : "Pending";
            }

            switch (match.RoundKey)
            {
                case "U-R16":
                    next = matches.FirstOrDefault(item => item.RoundKey == "U-QF" && item.MatchNumber == (match.MatchNumber + 1) / 2);
                    Place(next, winnerId, match.MatchNumber % 2 == 1);
                    next = matches.FirstOrDefault(item => item.RoundKey == "L-R1" && item.MatchNumber == (match.MatchNumber + 1) / 2);
                    Place(next, loserId, match.MatchNumber % 2 == 1);
                    break;
                case "U-QF":
                    next = matches.FirstOrDefault(item => item.RoundKey == "U-SF" && item.MatchNumber == (match.MatchNumber + 1) / 2);
                    Place(next, winnerId, match.MatchNumber % 2 == 1);
                    next = matches.FirstOrDefault(item => item.RoundKey == "L-R2" && item.MatchNumber == match.MatchNumber);
                    Place(next, loserId, false);
                    break;
                case "U-SF":
                    next = matches.FirstOrDefault(item => item.RoundKey == "U-F" && item.MatchNumber == 1);
                    Place(next, winnerId, match.MatchNumber == 1);
                    next = matches.FirstOrDefault(item => item.RoundKey == "L-R4" && item.MatchNumber == match.MatchNumber);
                    Place(next, loserId, false);
                    break;
                case "U-F":
                    Place(matches.FirstOrDefault(item => item.RoundKey == "GF" && item.MatchNumber == 1), winnerId, true);
                    Place(matches.FirstOrDefault(item => item.RoundKey == "L-F" && item.MatchNumber == 1), loserId, false);
                    break;
                case "L-R1":
                    Place(matches.FirstOrDefault(item => item.RoundKey == "L-R2" && item.MatchNumber == match.MatchNumber), winnerId, true);
                    break;
                case "L-R2":
                    next = matches.FirstOrDefault(item => item.RoundKey == "L-R3" && item.MatchNumber == (match.MatchNumber + 1) / 2);
                    Place(next, winnerId, match.MatchNumber % 2 == 1);
                    break;
                case "L-R3":
                    Place(matches.FirstOrDefault(item => item.RoundKey == "L-R4" && item.MatchNumber == match.MatchNumber), winnerId, true);
                    break;
                case "L-R4":
                    next = matches.FirstOrDefault(item => item.RoundKey == "L-R5" && item.MatchNumber == 1);
                    Place(next, winnerId, match.MatchNumber == 1);
                    break;
                case "L-R5":
                    Place(matches.FirstOrDefault(item => item.RoundKey == "L-F" && item.MatchNumber == 1), winnerId, true);
                    break;
                case "L-F":
                    Place(matches.FirstOrDefault(item => item.RoundKey == "GF" && item.MatchNumber == 1), winnerId, false);
                    break;
            }

            await _leagueRepository.SaveChangesAsync();
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

        private static bool CanManageTeamForUser(Team team, Guid callerId, ClaimsPrincipal? user)
        {
            var isAdmin = user?.IsInRole("Admin") == true || user?.Claims.Any(c => c.Type == "role" && c.Value == "Admin") == true;
            var isOwner = team.OwnerId == callerId;
            var isCaptain = team.Players?.Any(player => player.UserId == callerId && player.IsCaptain) == true;
            return isAdmin || isOwner || isCaptain;
        }

        private async Task<Team?> GetCallerMatchTeamAsync(LeagueMatch match)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var callerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
            {
                return null;
            }

            var teamA = match.TeamAId.HasValue ? await _teamRepository.GetTeamByIdAsync(match.TeamAId.Value) : null;
            var teamB = match.TeamBId.HasValue ? await _teamRepository.GetTeamByIdAsync(match.TeamBId.Value) : null;

            if (teamA != null && CanManageTeamForUser(teamA, callerId, user)) return teamA;
            if (teamB != null && CanManageTeamForUser(teamB, callerId, user)) return teamB;
            return null;
        }

        private async Task<string> SaveProofImageAsync(Guid matchId, Guid teamId, IFormFile image)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var maxFileSize = 6 * 1024 * 1024;
            var fileExt = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!image.ContentType.StartsWith("image/") || !allowedExtensions.Contains(fileExt))
            {
                throw new InvalidOperationException("Invalid file type. Only image files are allowed.");
            }
            if (image.Length > maxFileSize)
            {
                throw new InvalidOperationException("File too large. Maximum allowed size is 6 MB.");
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "league-proofs");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{matchId}-{teamId}-{DateTime.UtcNow:yyyyMMddHHmmss}{fileExt}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return Path.Combine("images", "league-proofs", fileName).Replace("\\", "/");
        }
    }
}
