using System.Security.Claims;
using NKZAPI.Models;

namespace NKZAPI.Dtos
{
    public static class PublicDtoMapper
    {
        public static PlayerPublicDto ToPublicDto(this Player player)
        {
            return new PlayerPublicDto
            {
                Id = player.Id,
                UserId = player.UserId,
                TeamId = player.TeamId,
                IsCaptain = player.IsCaptain,
                MainRole = player.MainRole,
                LookingForTeam = player.LookingForTeam,
                Tags = player.Tags,
                ProfileImageUrl = player.ProfileImageUrl,
                DiscordUsername = player.DiscordUsername,
                SummonerName = player.SummonerName,
                SummonerLevel = player.SummonerLevel,
                SoloQueueTier = player.SoloQueueTier,
                SoloQueueRank = player.SoloQueueRank,
                SoloQueueLP = player.SoloQueueLP,
                TotalMatches = player.TotalMatches,
                Wins = player.Wins,
                Losses = player.Losses,
                LastStatsUpdate = player.LastStatsUpdate,
                IsVerified = player.IsVerified,
                IsActive = player.IsActive,
                ChampionStats = player.ChampionStats ?? new List<PlayerChampionStat>(),
                RoleStats = player.RoleStats ?? new List<PlayerRoleStat>(),
                MatchHistory = player.MatchHistory ?? new List<PlayerMatchHistory>()
            };
        }

        public static TeamPublicDto ToPublicDto(this Team team)
        {
            return new TeamPublicDto
            {
                Id = team.Id,
                Name = team.Name,
                Tag = team.Tag,
                OwnerId = team.OwnerId,
                IsRecruiting = team.IsRecruiting,
                Points = team.Points,
                ProfileImageUrl = team.ProfileImageUrl,
                Players = (team.Players ?? new List<Player>()).Select(player => player.ToPublicDto()).ToList()
            };
        }

        public static LeaguePublicDto ToPublicDto(this League league, ClaimsPrincipal? user = null)
        {
            return new LeaguePublicDto
            {
                Id = league.Id,
                Name = league.Name,
                Teams = (league.Teams ?? new List<Team>()).Select(team => team.ToPublicDto()).ToList(),
                Award = league.Award,
                EntryFee = league.EntryFee,
                ImageUrl = league.ImageUrl,
                MaxTeams = league.MaxTeams,
                MinimumElo = league.MinimumElo,
                MaximumElo = league.MaximumElo,
                MinimumTeamPoints = league.MinimumTeamPoints,
                MaximumTeamPoints = league.MaximumTeamPoints,
                RankingQueueOpenTime = league.RankingQueueOpenTime,
                RankingQueueCloseTime = league.RankingQueueCloseTime,
                StartDate = league.StartDate,
                EndDate = league.EndDate,
                Modality = league.Modality,
                Matches = (league.Matches ?? new List<LeagueMatch>()).Select(match => match.ToPublicDto(league, user)).ToList(),
                Standings = league.Standings ?? new List<LeagueStanding>(),
                QueueEntries = league.QueueEntries ?? new List<LeagueQueueEntry>()
            };
        }

        public static LeagueMatchPublicDto ToPublicDto(this LeagueMatch match, League league, ClaimsPrincipal? user = null)
        {
            var visibleTeamIds = GetVisibleTeamIds(league, user);
            var canSeeMatchPrivateData =
                IsAdmin(user) ||
                (match.TeamAId.HasValue && visibleTeamIds.Contains(match.TeamAId.Value)) ||
                (match.TeamBId.HasValue && visibleTeamIds.Contains(match.TeamBId.Value));

            return new LeagueMatchPublicDto
            {
                Id = match.Id,
                LeagueId = match.LeagueId,
                Bracket = match.Bracket,
                RoundKey = match.RoundKey,
                RoundName = match.RoundName,
                WeekNumber = match.WeekNumber,
                MatchNumber = match.MatchNumber,
                BestOf = match.BestOf,
                TeamAId = match.TeamAId,
                TeamBId = match.TeamBId,
                WinnerTeamId = match.WinnerTeamId,
                LoserTeamId = match.LoserTeamId,
                TeamAScore = match.TeamAScore,
                TeamBScore = match.TeamBScore,
                AccessCode = canSeeMatchPrivateData ? match.AccessCode : "",
                ScheduledAt = match.ScheduledAt,
                ProposedScheduledAt = match.ProposedScheduledAt,
                ProposedByTeamId = match.ProposedByTeamId,
                ScheduleStatus = match.ScheduleStatus,
                CompletedAt = match.CompletedAt,
                Status = match.Status,
                Reports = (match.Reports ?? new List<LeagueMatchReport>())
                    .Where(report => canSeeMatchPrivateData || report.TeamId == match.WinnerTeamId)
                    .Select(report => report.ToPublicDto(canSeeMatchPrivateData))
                    .ToList()
            };
        }

        public static LeagueMatchReportPublicDto ToPublicDto(this LeagueMatchReport report, bool includeProofImage)
        {
            return new LeagueMatchReportPublicDto
            {
                Id = report.Id,
                LeagueMatchId = report.LeagueMatchId,
                TeamId = report.TeamId,
                ReportedWinnerTeamId = report.ReportedWinnerTeamId,
                ProofImageUrl = includeProofImage ? report.ProofImageUrl : "",
                UpdatedAt = report.UpdatedAt
            };
        }

        private static HashSet<Guid> GetVisibleTeamIds(League league, ClaimsPrincipal? user)
        {
            var userId = GetUserId(user);
            if (!userId.HasValue) return new HashSet<Guid>();

            return (league.Teams ?? new List<Team>())
                .Where(team =>
                    team.OwnerId == userId.Value ||
                    (team.Players ?? new List<Player>()).Any(player => player.UserId == userId.Value))
                .Select(team => team.Id)
                .ToHashSet();
        }

        private static Guid? GetUserId(ClaimsPrincipal? user)
        {
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;
            return Guid.TryParse(claim, out var userId) ? userId : null;
        }

        private static bool IsAdmin(ClaimsPrincipal? user)
        {
            return user?.IsInRole("Admin") == true || user?.Claims.Any(c => c.Type == "role" && c.Value == "Admin") == true;
        }
    }
}
