
using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Repositories;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NKZAPI.Services.LeagueServices
{
    public class LeagueServices : ILeagueInterface
    {
        private readonly LeagueRepository _leagueRepository;
        private readonly TeamRepository _teamRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly NKZAPIContext _context;

        public LeagueServices(LeagueRepository leagueRepository, TeamRepository teamRepository, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IConfiguration configuration, NKZAPIContext context)
        {
            _leagueRepository = leagueRepository;
            _teamRepository = teamRepository;
            _httpContextAccessor = httpContextAccessor;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
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
                league.MaximumTeamPoints = league.MaximumTeamPoints <= 0 ? 999999 : league.MaximumTeamPoints;
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
                existing.MinimumTeamPoints = league.MinimumTeamPoints;
                existing.MaximumTeamPoints = league.MaximumTeamPoints <= 0 ? 999999 : league.MaximumTeamPoints;
                existing.RankingQueueOpenTime = league.RankingQueueOpenTime;
                existing.RankingQueueCloseTime = league.RankingQueueCloseTime;
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
            await RefundLeaguePaymentsToWalletAsync(league.Id);
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

                if (!IsRankingLeague(league) && league.Matches.Any())
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

                if (!TeamMatchesPointRange(league, team))
                {
                    response.Success = false;
                    response.Message = $"O time precisa ter entre {league.MinimumTeamPoints} e {league.MaximumTeamPoints} pontos para entrar nesta liga.";
                    return response;
                }

                if (league.EntryFee > 0 && await _leagueRepository.GetApprovedLeaguePaymentAsync(leagueId, teamId) == null)
                {
                    var paymentResponse = await PayLeagueEntryAsync(league, team);
                    if (paymentResponse.Success && paymentResponse.Data == "wallet-paid")
                    {
                        await AddTeamToLeagueAfterApprovalAsync(leagueId, teamId);
                        response.Success = true;
                        response.Message = "Entrada paga com saldo da carteira. Time inscrito na liga.";
                        response.Data = teamId.ToString();
                        return response;
                    }

                    response.Success = paymentResponse.Success;
                    response.Message = paymentResponse.Message;
                    response.Data = paymentResponse.Data;
                    return response;
                }

                await AddTeamToLeagueAfterApprovalAsync(leagueId, teamId);
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

        public async Task<Response<string>> ConfirmLeaguePaymentAsync(Guid paymentId)
        {
            var response = new Response<string>();
            try
            {
                var payment = await _leagueRepository.GetLeaguePaymentByIdAsync(paymentId);
                if (payment == null)
                {
                    response.Success = false;
                    response.Message = "Pagamento nao encontrado.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                {
                    return await ProcessMercadoPagoNotificationAsync(payment.ProviderPaymentId);
                }

                response.Success = payment.Status == "Approved";
                response.Message = payment.Status == "Approved"
                    ? "Pagamento aprovado."
                    : "Pagamento ainda nao foi aprovado.";
                response.Data = payment.CheckoutUrl ?? payment.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Erro ao confirmar pagamento: {ex.Message}";
            }

            return response;
        }

        public async Task<Response<string>> ProcessMercadoPagoNotificationAsync(string? paymentId)
        {
            var response = new Response<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(paymentId))
                {
                    response.Success = false;
                    response.Message = "Payment id is required.";
                    return response;
                }

                var info = await GetMercadoPagoPaymentAsync(paymentId);
                if (info == null)
                {
                    response.Success = false;
                    response.Message = "Pagamento nao encontrado no Mercado Pago.";
                    return response;
                }

                var payment = Guid.TryParse(info.ExternalReference, out var internalPaymentId)
                    ? await _leagueRepository.GetLeaguePaymentByIdAsync(internalPaymentId)
                    : await _leagueRepository.GetLeaguePaymentByProviderPaymentIdAsync(info.PaymentId);

                if (payment == null)
                {
                    response.Success = false;
                    response.Message = "Pagamento local nao encontrado.";
                    return response;
                }

                payment.ProviderPaymentId = info.PaymentId;
                payment.Status = ToLocalPaymentStatus(info.Status);
                payment.UpdatedAt = DateTime.UtcNow;

                if (payment.Status == "Approved")
                {
                    payment.ApprovedAt ??= DateTime.UtcNow;
                    await AddTeamToLeagueAfterApprovalAsync(payment.LeagueId, payment.TeamId);
                    response.Message = "Pagamento aprovado. Time inscrito na liga.";
                }
                else if (payment.Status == "Rejected")
                {
                    await RefundRejectedLeaguePaymentWalletCreditAsync(payment);
                    response.Message = "Pagamento rejeitado. Credito da carteira devolvido, se havia sido usado.";
                }
                else
                {
                    response.Message = $"Pagamento atualizado: {payment.Status}.";
                }

                await _leagueRepository.SaveChangesAsync();
                response.Success = true;
                response.Data = payment.Id.ToString();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Erro ao processar pagamento: {ex.Message}";
            }

            return response;
        }

        private async Task RefundRejectedLeaguePaymentWalletCreditAsync(LeaguePayment payment)
        {
            if (!payment.CreatedByUserId.HasValue || payment.WalletCreditUsed <= 0) return;

            var alreadyRefunded = await _context.WalletTransactions.AnyAsync(transaction =>
                transaction.LeaguePaymentId == payment.Id &&
                transaction.UserId == payment.CreatedByUserId.Value &&
                transaction.Type == "Refund");

            if (alreadyRefunded) return;

            var debitExists = await _context.WalletTransactions.AnyAsync(transaction =>
                transaction.LeaguePaymentId == payment.Id &&
                transaction.UserId == payment.CreatedByUserId.Value &&
                transaction.Type == "Debit" &&
                transaction.Amount < 0);

            if (!debitExists) return;

            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == payment.CreatedByUserId.Value);
            if (user == null) return;

            user.WalletBalance += payment.WalletCreditUsed;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = user.Id,
                Amount = payment.WalletCreditUsed,
                Type = "Refund",
                Description = "Credito devolvido por pagamento de liga rejeitado",
                LeagueId = payment.LeagueId,
                TeamId = payment.TeamId,
                LeaguePaymentId = payment.Id
            });
        }

        private async Task RefundLeaguePaymentsToWalletAsync(Guid leagueId)
        {
            var payments = await _leagueRepository.GetApprovedLeaguePaymentsByLeagueAsync(leagueId);
            foreach (var payment in payments.Where(item => item.CreatedByUserId.HasValue && item.TotalAmount > 0 && item.Status == "Approved"))
            {
                var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == payment.CreatedByUserId!.Value);
                if (user == null) continue;

                user.WalletBalance += payment.TotalAmount;
                payment.Status = "Refunded";
                payment.UpdatedAt = DateTime.UtcNow;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = user.Id,
                    Amount = payment.TotalAmount,
                    Type = "Refund",
                    Description = "Credito por cancelamento de liga",
                    LeagueId = payment.LeagueId,
                    TeamId = payment.TeamId,
                    LeaguePaymentId = payment.Id
                });
            }

            await _context.SaveChangesAsync();
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

                if (!IsRankingLeague(league) && league.Matches.Any())
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

            if (IsRankingLeague(league))
            {
                response.Success = false;
                response.Message = "Ligas de ranking nao possuem chaveamento.";
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

        public async Task<Response<string>> JoinRankingQueueAsync(Guid leagueId, Guid teamId)
        {
            var response = new Response<string>();
            var league = await _leagueRepository.GetLeagueByIdAsync(leagueId);
            if (league == null)
            {
                response.Success = false;
                response.Message = "League not found.";
                return response;
            }

            if (!IsRankingLeague(league))
            {
                response.Success = false;
                response.Message = "A fila esta disponivel apenas para ligas de ranking.";
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

            if (!league.Teams.Any(item => item.Id == teamId))
            {
                response.Success = false;
                response.Message = "Seu time precisa estar inscrito nesta liga antes de entrar na fila.";
                return response;
            }

            if (!TeamMatchesPointRange(league, team))
            {
                response.Success = false;
                response.Message = $"O time precisa ter entre {league.MinimumTeamPoints} e {league.MaximumTeamPoints} pontos para jogar esta liga.";
                return response;
            }

            if (!IsLeagueOpenByDates(league))
            {
                response.Success = false;
                response.Message = "A liga nao esta dentro do periodo ativo.";
                return response;
            }

            if (!IsQueueWindowOpen(league))
            {
                response.Success = false;
                response.Message = league.RankingQueueOpenTime.HasValue
                    ? $"A fila abre as {league.RankingQueueOpenTime.Value:hh\\:mm}."
                    : "A fila ainda nao possui horario configurado.";
                return response;
            }

            if (HasActiveRankingMatch(league, teamId))
            {
                response.Success = false;
                response.Message = "Finalize sua partida atual antes de entrar na fila novamente.";
                return response;
            }

            if (await _leagueRepository.GetWaitingQueueEntryAsync(leagueId, teamId) != null)
            {
                response.Success = false;
                response.Message = "Seu time ja esta na fila.";
                return response;
            }

            var entry = await _leagueRepository.AddQueueEntryAsync(new LeagueQueueEntry
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                TeamId = teamId,
                Status = "Waiting",
                JoinedAt = DateTime.UtcNow
            });

            var waiting = await _leagueRepository.GetWaitingQueueEntriesAsync(leagueId);
            if (waiting.Count >= 2)
            {
                var first = entry;
                var second = waiting.First(item => item.TeamId != teamId);
                var match = await CreateRankingMatchAsync(league, first, second);

                response.Success = true;
                response.Message = $"Partida encontrada. Codigo de acesso: {match.AccessCode}";
                response.Data = match.Id.ToString();
                return response;
            }

            response.Success = true;
            response.Message = "Seu time entrou na fila. Aguardando outro time.";
            response.Data = entry.Id.ToString();
            return response;
        }

        public async Task<Response<string>> LeaveRankingQueueAsync(Guid leagueId, Guid teamId)
        {
            var response = new Response<string>();
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

            var entry = await _leagueRepository.GetWaitingQueueEntryAsync(leagueId, teamId);
            if (entry == null)
            {
                response.Success = false;
                response.Message = "Seu time nao esta na fila.";
                return response;
            }

            entry.Status = "Cancelled";
            await _leagueRepository.SaveChangesAsync();

            response.Success = true;
            response.Message = "Seu time saiu da fila.";
            response.Data = teamId.ToString();
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
            if (!IsRankingMatch(match))
            {
                await AdvanceBracketAsync(match, winnerId, loserId);
            }
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
                AccessCode = "",
                ScheduledAt = startDate.Date.AddDays((week - 1) * 7)
            };
        }

        private async Task<LeagueMatch> CreateRankingMatchAsync(League league, LeagueQueueEntry first, LeagueQueueEntry second)
        {
            var match = new LeagueMatch
            {
                Id = Guid.NewGuid(),
                LeagueId = league.Id,
                Bracket = "Ranking",
                RoundKey = "RANKING",
                RoundName = "Partida de ranking",
                WeekNumber = Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow.Date - (league.StartDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date)).TotalDays / 7.0) + 1),
                MatchNumber = league.Matches.Count(item => item.Bracket == "Ranking") + 1,
                BestOf = 1,
                TeamAId = first.TeamId,
                TeamBId = second.TeamId,
                ScheduledAt = DateTime.UtcNow,
                ScheduleStatus = "Confirmed",
                Status = "Ready",
                AccessCode = GenerateAccessCode()
            };

            var created = await _leagueRepository.AddMatchAsync(match);
            first.Status = "Matched";
            first.MatchedAt = DateTime.UtcNow;
            first.MatchId = created.Id;
            second.Status = "Matched";
            second.MatchedAt = DateTime.UtcNow;
            second.MatchId = created.Id;
            await _leagueRepository.SaveChangesAsync();
            return created;
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
            return team.Points > 0 ? team.Points : (team.Players ?? new List<Player>()).Sum(GetPlayerSeedScore);
        }

        private static double GetPlayerSeedScore(Player player)
        {
            var rankPoints = GetRankPoints(player.SoloQueueTier, player.SoloQueueRank, player.SoloQueueLP);
            var winRate = player.Wins + player.Losses > 0
                ? (double)player.Wins / (player.Wins + player.Losses)
                : 0;

            return rankPoints + winRate;
        }

        private static int GetRankPoints(string? tier, string? rank, int lp)
        {
            var normalizedTier = (tier ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToUpperInvariant() ?? "";
            var normalizedRank = (rank ?? "").Trim().ToUpperInvariant();
            var leaguePoints = Math.Max(0, lp);

            if (normalizedTier == "MASTER") return leaguePoints >= 300 ? 150 : 100;

            return normalizedTier switch
            {
                "IRON" => normalizedRank switch
                {
                    "IV" => 1,
                    "III" => 2,
                    "II" => 3,
                    "I" => 4,
                    _ => 0
                },
                "BRONZE" => normalizedRank switch
                {
                    "IV" => 5,
                    "III" => 6,
                    "II" => 7,
                    "I" => 8,
                    _ => 0
                },
                "SILVER" => normalizedRank switch
                {
                    "IV" => 10,
                    "III" => 11,
                    "II" => 12,
                    "I" => 13,
                    _ => 0
                },
                "GOLD" => normalizedRank switch
                {
                    "IV" => 20,
                    "III" => 21,
                    "II" => 22,
                    "I" => 23,
                    _ => 0
                },
                "PLATINUM" => normalizedRank switch
                {
                    "IV" => 30,
                    "III" => 31,
                    "II" => 32,
                    "I" => 33,
                    _ => 0
                },
                "EMERALD" => normalizedRank switch
                {
                    "IV" => 40,
                    "III" => 41,
                    "II" => 42,
                    "I" => 43,
                    _ => 0
                },
                "DIAMOND" => normalizedRank switch
                {
                    "IV" => 55,
                    "III" => 60,
                    "II" => 70,
                    "I" => 80,
                    _ => 0
                },
                "GRANDMASTER" => 175,
                "CHALLENGER" => 200,
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

        private static bool IsRankingLeague(League league)
        {
            return string.Equals(league.Modality, "Ranking", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRankingMatch(LeagueMatch match)
        {
            return string.Equals(match.Bracket, "Ranking", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(match.RoundKey, "RANKING", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasActiveRankingMatch(League league, Guid teamId)
        {
            return league.Matches.Any(match =>
                IsRankingMatch(match) &&
                !string.Equals(match.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                (match.TeamAId == teamId || match.TeamBId == teamId));
        }

        private async Task<Response<string>> PayLeagueEntryAsync(League league, Team team)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var callerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;
            if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
            {
                return new Response<string> { Success = false, Message = "Unauthorized" };
            }

            var existing = await _leagueRepository.GetPendingLeaguePaymentAsync(league.Id, team.Id);
            if (existing != null && !string.IsNullOrWhiteSpace(existing.CheckoutUrl))
            {
                return new Response<string>
                {
                    Success = true,
                    Message = "Pagamento pendente. Abra o checkout para concluir a inscricao.",
                    Data = existing.CheckoutUrl
                };
            }

            var entryFee = Convert.ToDecimal(league.EntryFee);
            var account = await _context.Users.FirstOrDefaultAsync(item => item.Id == callerId);
            if (account == null)
            {
                return new Response<string> { Success = false, Message = "Usuario nao encontrado." };
            }

            var walletCreditUsed = Math.Min(account.WalletBalance, entryFee);
            var remaining = entryFee - walletCreditUsed;

            if (remaining <= 0)
            {
                account.WalletBalance -= walletCreditUsed;
                var walletPayment = await _leagueRepository.AddLeaguePaymentAsync(new LeaguePayment
                {
                    Id = Guid.NewGuid(),
                    LeagueId = league.Id,
                    TeamId = team.Id,
                    CreatedByUserId = callerId,
                    Amount = 0,
                    TotalAmount = entryFee,
                    WalletCreditUsed = walletCreditUsed,
                    Status = "Approved",
                    Provider = "Wallet",
                    ApprovedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = callerId,
                    Amount = -walletCreditUsed,
                    Type = "Debit",
                    Description = $"Inscricao na liga {league.Name}",
                    LeagueId = league.Id,
                    TeamId = team.Id,
                    LeaguePaymentId = walletPayment.Id
                });
                await _context.SaveChangesAsync();

                return new Response<string> { Success = true, Message = "Entrada paga com saldo da carteira.", Data = "wallet-paid" };
            }

            var paymentResponse = await CreateOrReuseLeaguePaymentAsync(league, team, callerId, remaining, entryFee, walletCreditUsed);
            if (walletCreditUsed > 0 && paymentResponse.Success)
            {
                var pendingPayment = await _leagueRepository.GetPendingLeaguePaymentAsync(league.Id, team.Id);
                account.WalletBalance -= walletCreditUsed;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = callerId,
                    Amount = -walletCreditUsed,
                    Type = "Debit",
                    Description = $"Credito usado na inscricao da liga {league.Name}",
                    LeagueId = league.Id,
                    TeamId = team.Id,
                    LeaguePaymentId = pendingPayment?.Id
                });
                await _context.SaveChangesAsync();
            }

            return paymentResponse;
        }

        private async Task<Response<string>> CreateOrReuseLeaguePaymentAsync(League league, Team team, Guid callerId, decimal amount, decimal totalAmount, decimal walletCreditUsed)
        {
            var response = new Response<string>();
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                response.Success = false;
                response.Message = "Mercado Pago nao esta configurado. Defina MercadoPago__AccessToken.";
                return response;
            }

            var payment = await _leagueRepository.AddLeaguePaymentAsync(new LeaguePayment
            {
                Id = Guid.NewGuid(),
                LeagueId = league.Id,
                TeamId = team.Id,
                CreatedByUserId = callerId,
                Amount = amount,
                TotalAmount = totalAmount,
                WalletCreditUsed = walletCreditUsed,
                Status = "Pending"
            });

            var checkoutUrl = await CreateMercadoPagoPreferenceAsync(league, team, payment, accessToken);
            payment.CheckoutUrl = checkoutUrl.Url;
            payment.ProviderPreferenceId = checkoutUrl.PreferenceId;
            payment.UpdatedAt = DateTime.UtcNow;
            await _leagueRepository.SaveChangesAsync();

            response.Success = true;
            response.Message = "Pagamento necessario para entrar nesta liga.";
            response.Data = payment.CheckoutUrl;
            return response;
        }

        private async Task<(string PreferenceId, string Url)> CreateMercadoPagoPreferenceAsync(League league, Team team, LeaguePayment payment, string accessToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var apiBaseUrl = (_configuration["Api:BaseUrl"] ?? _configuration["App:BaseUrl"])?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(apiBaseUrl) && httpContext != null)
            {
                apiBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            }

            var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var leagueUrl = $"{frontendBaseUrl}/leagues/{league.Id}";
            var payload = new Dictionary<string, object?>
            {
                ["items"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["title"] = $"Inscricao {league.Name} - {team.Name}",
                        ["quantity"] = 1,
                        ["currency_id"] = "BRL",
                        ["unit_price"] = payment.Amount
                    }
                },
                ["external_reference"] = payment.Id.ToString(),
                ["notification_url"] = $"{apiBaseUrl}/api/league/payments/mercadopago/webhook",
                ["back_urls"] = new Dictionary<string, object?>
                {
                    ["success"] = $"{leagueUrl}?payment=success",
                    ["failure"] = $"{leagueUrl}?payment=failure",
                    ["pending"] = $"{leagueUrl}?payment=pending"
                },
                ["auto_return"] = "approved"
            };

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var result = await client.SendAsync(request);
            var body = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)result.StatusCode}: {body}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var preferenceId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
            var initPoint = root.TryGetProperty("init_point", out var initPointElement) ? initPointElement.GetString() : null;
            var sandboxInitPoint = root.TryGetProperty("sandbox_init_point", out var sandboxElement) ? sandboxElement.GetString() : null;
            var url = accessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(sandboxInitPoint)
                ? sandboxInitPoint
                : initPoint;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Mercado Pago nao retornou URL de checkout.");
            }

            return (preferenceId, url);
        }

        private async Task<MercadoPagoPaymentInfo?> GetMercadoPagoPaymentAsync(string paymentId)
        {
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("Mercado Pago nao esta configurado. Defina MercadoPago__AccessToken.");
            }

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var result = await client.SendAsync(request);
            var body = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)result.StatusCode}: {body}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var id = root.TryGetProperty("id", out var idElement) ? idElement.GetRawText().Trim('"') : paymentId;
            var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? "" : "";
            var externalReference = root.TryGetProperty("external_reference", out var referenceElement) ? referenceElement.GetString() : null;
            return new MercadoPagoPaymentInfo(id, status, externalReference);
        }

        private async Task AddTeamToLeagueAfterApprovalAsync(Guid leagueId, Guid teamId)
        {
            var league = await _leagueRepository.GetLeagueByIdAsync(leagueId) ?? throw new InvalidOperationException("League not found.");
            if (league.Teams.Any(team => team.Id == teamId)) return;
            if (league.Teams.Count >= league.MaxTeams)
            {
                throw new InvalidOperationException("League has reached its maximum number of teams.");
            }

            var team = await _teamRepository.GetTeamByIdAsync(teamId) ?? throw new InvalidOperationException("Team not found.");
            await _leagueRepository.AddTeamToLeagueAsync(leagueId, team);
            var updatedLeague = await _leagueRepository.GetLeagueByIdAsync(leagueId);
            if (updatedLeague != null && !IsRankingLeague(updatedLeague) && updatedLeague.Teams.Count == 16 && !updatedLeague.Matches.Any())
            {
                await GeneratePlayoffAsync(leagueId);
            }
        }

        private static string ToLocalPaymentStatus(string? mercadoPagoStatus)
        {
            return string.Equals(mercadoPagoStatus, "approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : string.Equals(mercadoPagoStatus, "rejected", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(mercadoPagoStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "Rejected"
                    : "Pending";
        }

        private static bool TeamMatchesPointRange(League league, Team team)
        {
            var max = league.MaximumTeamPoints <= 0 ? 999999 : league.MaximumTeamPoints;
            return team.Points >= league.MinimumTeamPoints && team.Points <= max;
        }

        private static bool IsLeagueOpenByDates(League league)
        {
            var brazilOffset = TimeSpan.FromHours(-3);
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(brazilOffset).DateTime);
            var start = league.StartDate.HasValue
                ? DateOnly.FromDateTime(new DateTimeOffset(DateTime.SpecifyKind(league.StartDate.Value, DateTimeKind.Utc)).ToOffset(brazilOffset).DateTime)
                : (DateOnly?)null;
            var end = league.EndDate.HasValue
                ? DateOnly.FromDateTime(new DateTimeOffset(DateTime.SpecifyKind(league.EndDate.Value, DateTimeKind.Utc)).ToOffset(brazilOffset).DateTime)
                : (DateOnly?)null;

            return (!start.HasValue || today >= start.Value) && (!end.HasValue || today <= end.Value);
        }

        private static bool IsQueueWindowOpen(League league)
        {
            if (!league.RankingQueueOpenTime.HasValue) return false;

            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).TimeOfDay;
            var open = league.RankingQueueOpenTime.Value;
            var close = league.RankingQueueCloseTime ?? open.Add(TimeSpan.FromHours(4));
            return close.TotalHours >= 24
                ? now >= open || now <= close.Subtract(TimeSpan.FromHours(24))
                : now >= open && now <= close;
        }

        private static string GenerateAccessCode()
        {
            return $"NKZ-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
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

        private sealed record MercadoPagoPaymentInfo(string PaymentId, string Status, string? ExternalReference);
    }
}
