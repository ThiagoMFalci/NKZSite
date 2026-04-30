using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Repositories;
using NKZAPI.Services.RiotService;
using NKZAPI.Services.TeamServices;

namespace NKZAPI.Services.PlayerServices
{
    public class PlayerServices : IPlayerInterface
    {
        private readonly PlayerRepository _context;
        private readonly IRiotService _riotService;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly ITeamInterface _teamService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        public PlayerServices(PlayerRepository context, IRiotService riotService, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, ITeamInterface teamServices, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _riotService = riotService;
            _httpContextAccessor = httpContextAccessor;
            _teamService = teamServices;
            _env = env;
        }

        public async Task<Response<Player>> UpdatePlayerFromRiotAsync(Guid userId, string summonerName, string region = "br1")
        {
            var response = new Response<Player>();
            try
            {
                var userExists = await _context.UserExistsAsync(userId);
                if (!userExists)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    return response;
                }

                var riotId = ParseRiotId(summonerName);
                if (riotId == null)
                {
                    response.Success = false;
                    response.Message = "Informe o Riot ID no formato Nome#TAG, por exemplo Thiago#BR1.";
                    return response;
                }

                var regionalRoute = GetRegionalRoute(region);
                var account = await _riotService.GetAccountByRiotIdAsync(regionalRoute, riotId.Value.GameName, riotId.Value.TagLine);
                if (account == null)
                {
                    response.Success = false;
                    response.Message = "Riot ID not found.";
                    return response;
                }

                var summoner = await _riotService.GetSummonerByPuuidAsync(region, account.Puuid);
                if (summoner == null)
                {
                    response.Success = false;
                    response.Message = "Summoner not found on this League region.";
                    return response;
                }

                var riotDisplayName = $"{account.GameName}#{account.TagLine}";
                var player = await _context.GetPlayerForUserByRiotIdentityAsync(userId, account.Puuid, riotDisplayName);
                var isNewPlayer = player == null;

                if (isNewPlayer)
                {
                    player = new Player
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        SummonerName = riotDisplayName,
                        RiotPuuid = account.Puuid,
                        IsCaptain = false,
                        IsActive = true,
                        LastStatsUpdate = DateTime.UtcNow
                    };
                }

                player.RiotPuuid = account.Puuid;
                player.SummonerLevel = (int)summoner.SummonerLevel;
                player.SummonerName = riotDisplayName;

                var leagueEntry = await _riotService.GetSoloQueueEntryByPuuidAsync(region, account.Puuid);
                if (leagueEntry != null)
                {
                    player.SoloQueueTier = leagueEntry.Tier ?? "UNRANKED";
                    player.SoloQueueRank = leagueEntry.Rank ?? "";
                    player.SoloQueueLP = leagueEntry.LeaguePoints;
                    player.Wins = leagueEntry.Wins;
                    player.Losses = leagueEntry.Losses;
                    player.TotalMatches = leagueEntry.Wins + leagueEntry.Losses;
                    player.IsVerified = true;
                }

                player.LastStatsUpdate = DateTime.UtcNow;

                if (isNewPlayer)
                {
                    await _context.AddSyncedPlayerAsync(userId, player);
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                await SyncRecentPerformanceAsync(player.Id, account.Puuid, regionalRoute);

                response.Success = true;
                response.Message = "Player updated from Riot.";
                response.Data = await _context.GetPlayerByIdAsync(player.Id) ?? player;
            }
            catch (DbUpdateConcurrencyException)
            {
                response.Success = false;
                response.Message = "Nao foi possivel salvar o player porque o registro foi alterado ou removido. Tente sincronizar novamente.";
            }
            catch (HttpRequestException ex)
            {
                response.Success = false;
                response.Message = $"Riot API error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<Response<Player>> RefreshPlayerFromRiotAsync(Guid userId, string region = "br1")
        {
            var playerResponse = await GetPlayerByUserIdAsync(userId);
            if (!playerResponse.Success || playerResponse.Data == null)
            {
                return new Response<Player>
                {
                    Success = false,
                    Message = "Player not found.",
                    Data = null
                };
            }

            var player = playerResponse.Data;
            if (string.IsNullOrWhiteSpace(player.SummonerName))
            {
                return new Response<Player>
                {
                    Success = false,
                    Message = "Player does not have a Riot ID linked.",
                    Data = null
                };
            }

            return await UpdatePlayerFromRiotAsync(userId, player.SummonerName, region);
        }

        public async Task<Response<List<Player>>> GetAllPlayersAsync()
        {
            var response = new Response<List<Player>>();
            try
            {
                var players = await _context.GetAllPlayersAsync();
                if (players == null)
                {
                    response.Success = false;
                    response.Message = "Players not found.";
                    response.Data = null;
                    return response;
                }
                response.Success = true;
                response.Message = "Players retrieved successfully.";
                response.Data = players;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }

        public async Task<Response<Player>> GetPlayerByIdAsync(Guid id)
        {
            var response = new Response<Player>();
            try
            {
                var player = await _context.GetPlayerByIdAsync(id);
                if (player == null)
                {
                    response.Success = false;
                    response.Message = "Player not found.";
                    response.Data = null;
                    return response;
                }
                response.Success = true;
                response.Message = "Player retrieved successfully.";
                response.Data = player;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }

        }
        public async Task<Response<Player>> DeletePlayerAsync(Guid playerid)
        {
            var response = new Response<Player>();
            try
            {
                var player = await _context.GetPlayerByIdAsync(playerid);
                if (player == null)
                {
                    response.Success = false;
                    response.Message = "Player not found.";
                    response.Data = null;
                    return response;
                }
                await _context.DeletePlayerAsync(playerid);
                response.Success = true;
                response.Message = "Player deleted successfully.";
                response.Data = player;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }

        }
        public async Task<Response<Player>> AddPlayerAsync(Guid UserId, Player player)
        {
            var response = new Response<Player>();

            try
            {
                var user = await _context.GetUserWithPlayersAsync(UserId);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    response.Data = null;
                    return response;
                }
                if (player == null)
                {
                    response.Success = false;
                    response.Message = "Player data is null.";
                    response.Data = null;
                    return response;
                }
                if (string.IsNullOrWhiteSpace(player.SummonerName))
                {
                    response.Success = false;
                    response.Message = "SummonerName is required.";
                    response.Data = null;
                    return response;
                }
                player.Id = Guid.NewGuid();
                player.UserId = UserId;
                player.TeamId = null;
                player.IsCaptain = false;
                player.RiotPuuid ??= "";
                player.MainRole = NormalizeRole(player.MainRole);
                player.Tags ??= "";
                await _context.AddPlayerAsync(UserId, player);
                response.Success = true;
                response.Message = "Player added successfully.";
                response.Data = player;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }


        }
        public async Task<Response<Player>> GetPlayerByUserIdAsync(Guid userId)
        {
            var response = new Response<Player>();
            try
            {
                var user = await _context.GetUserWithPlayersAsync(userId);
                if (user == null || user.Player == null || !user.Player.Any())
                {
                    response.Success = false;
                    response.Message = "Player not found for the given user.";
                    response.Data = null;
                    return response;
                }
                var player = user.Player.First();
                response.Success = true;
                response.Message = "Player retrieved successfully.";
                response.Data = player;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }


        public async Task<Response<Player>> UploadProfileImage(Guid playerId, IFormFile profileImageUrl)
        {
            var response = new Response<Player>();
            try
            {
                var player = await _context.GetPlayerByIdAsync(playerId);
                if (player == null)
                {
                    var user = await _context.GetUserWithPlayersAsync(playerId);
                    player = user?.Player?.FirstOrDefault();
                }

                if (player == null)
                {
                    response.Success = false;
                    response.Message = "Player not found.";
                    response.Data = null;
                    return response;
                }
                if (profileImageUrl == null || profileImageUrl.Length == 0)
                {
                    response.Success = false;
                    response.Message = "No file uploaded.";
                    response.Data = null;
                    return response;
                }
                // Validate content type (should be image/*) and extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var maxFileSize = 5 * 1024 * 1024; // 5 MB
                var fileExt = Path.GetExtension(profileImageUrl.FileName).ToLowerInvariant();
                if (!profileImageUrl.ContentType.StartsWith("image/") || !allowedExtensions.Contains(fileExt))
                {
                    response.Success = false;
                    response.Message = "Invalid file type. Only image files are allowed (jpg, jpeg, png, gif, bmp, webp).";
                    response.Data = null;
                    return response;
                }
                if (profileImageUrl.Length > maxFileSize)
                {
                    response.Success = false;
                    response.Message = "File too large. Maximum allowed size is 5 MB.";
                    response.Data = null;
                    return response;
                }
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{player.Id}{Path.GetExtension(profileImageUrl.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImageUrl.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("images", "profiles", fileName).Replace("\\", "/");
                await _context.UploadProfileImage(player.Id, relativePath);
                response.Success = true;
                response.Message = "Profile image uploaded successfully.";
                response.Data = await _context.GetPlayerByIdAsync(player.Id);
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }

        public async Task<Response<Player>> UpdateCompetitiveProfileAsync(Guid userId, PlayerCompetitiveProfileDto profile)
        {
            var response = new Response<Player>();
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var callerIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("Id")?.Value;
                if (string.IsNullOrWhiteSpace(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerId))
                {
                    response.Success = false;
                    response.Message = "Unauthorized";
                    return response;
                }

                var isAdmin = user?.IsInRole("Admin") == true || user?.Claims.Any(c => c.Type == "role" && c.Value == "Admin") == true;
                if (callerId != userId && !isAdmin)
                {
                    response.Success = false;
                    response.Message = "Forbidden";
                    return response;
                }

                var playerResponse = await GetPlayerByUserIdAsync(userId);
                if (!playerResponse.Success || playerResponse.Data == null)
                {
                    response.Success = false;
                    response.Message = "Player not found.";
                    return response;
                }

                var player = playerResponse.Data;
                var nextRole = NormalizeRole(profile.MainRole);
                var normalizedTags = (profile.Tags ?? new List<string>())
                    .Select(tag => tag.Trim())
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();

                player.MainRole = nextRole;
                player.LookingForTeam = profile.LookingForTeam;
                player.Tags = string.Join(",", normalizedTags);

                await _context.UpdateCompetitiveProfileAsync(player);

                response.Success = true;
                response.Message = "Competitive profile updated.";
                response.Data = player;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        private static string NormalizeRole(string? role)
        {
            var allowedRoles = new[] { "Top", "Jungle", "Mid", "ADC", "Support", "Flex" };
            return allowedRoles.FirstOrDefault(item => item.Equals(role, StringComparison.OrdinalIgnoreCase)) ?? "Flex";
        }

        private async Task SyncRecentPerformanceAsync(Guid playerId, string puuid, string regionalRoute)
        {
            var matchIds = await _riotService.GetRecentMatchIdsAsync(regionalRoute, puuid, 5);
            var matchHistory = new List<PlayerMatchHistory>();

            foreach (var matchId in matchIds)
            {
                using var match = await _riotService.GetMatchAsync(regionalRoute, matchId);
                if (match == null) continue;

                var root = match.RootElement;
                if (!root.TryGetProperty("info", out var info)) continue;
                if (!info.TryGetProperty("participants", out var participants)) continue;

                JsonElement? participant = null;
                foreach (var item in participants.EnumerateArray())
                {
                    if (GetString(item, "puuid").Equals(puuid, StringComparison.Ordinal))
                    {
                        participant = item;
                        break;
                    }
                }

                if (participant == null) continue;

                var playedAt = DateTimeOffset
                    .FromUnixTimeMilliseconds(GetLong(info, "gameCreation"))
                    .UtcDateTime;

                matchHistory.Add(new PlayerMatchHistory
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    PlayedAt = playedAt,
                    ChampionName = GetString(participant.Value, "championName", "Unknown"),
                    Role = NormalizeRiotRole(GetString(participant.Value, "teamPosition", GetString(participant.Value, "individualPosition", "Flex"))),
                    QueueType = NormalizeQueue(GetInt(info, "queueId")),
                    Win = GetBool(participant.Value, "win"),
                    Kills = GetInt(participant.Value, "kills"),
                    Deaths = GetInt(participant.Value, "deaths"),
                    Assists = GetInt(participant.Value, "assists")
                });
            }

            var championStats = matchHistory
                .GroupBy(match => match.ChampionName)
                .Select(group =>
                {
                    var wins = group.Count(match => match.Win);
                    var matches = group.Count();
                    return new PlayerChampionStat
                    {
                        Id = Guid.NewGuid(),
                        PlayerId = playerId,
                        ChampionName = group.Key,
                        Matches = matches,
                        Wins = wins,
                        Losses = matches - wins,
                        WinRate = matches == 0 ? 0 : Math.Round(wins * 100.0 / matches, 1)
                    };
                })
                .OrderByDescending(stat => stat.Matches)
                .ThenByDescending(stat => stat.WinRate)
                .ToList();

            var roleStats = matchHistory
                .GroupBy(match => match.Role)
                .Select(group =>
                {
                    var wins = group.Count(match => match.Win);
                    var matches = group.Count();
                    return new PlayerRoleStat
                    {
                        Id = Guid.NewGuid(),
                        PlayerId = playerId,
                        Role = group.Key,
                        Matches = matches,
                        Wins = wins,
                        Losses = matches - wins,
                        WinRate = matches == 0 ? 0 : Math.Round(wins * 100.0 / matches, 1)
                    };
                })
                .OrderByDescending(stat => stat.Matches)
                .ThenByDescending(stat => stat.WinRate)
                .ToList();

            await _context.ReplacePlayerPerformanceAsync(playerId, championStats, roleStats, matchHistory);
        }

        private static string NormalizeRiotRole(string role)
        {
            return role.ToUpperInvariant() switch
            {
                "TOP" => "Top",
                "JUNGLE" => "Jungle",
                "MIDDLE" or "MID" => "Mid",
                "BOTTOM" or "ADC" => "ADC",
                "UTILITY" or "SUPPORT" => "Support",
                _ => "Flex"
            };
        }

        private static string NormalizeQueue(int queueId)
        {
            return queueId switch
            {
                420 => "Ranqueada Solo/Duo",
                440 => "Ranqueada Flex",
                400 => "Normal Draft",
                430 => "Normal Blind",
                450 => "ARAM",
                _ => "Outra"
            };
        }

        private static string GetString(JsonElement element, string property, string fallback = "")
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? fallback
                : fallback;
        }

        private static int GetInt(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result)
                ? result
                : 0;
        }

        private static long GetLong(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result)
                ? result
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static bool GetBool(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
        }

        private static (string GameName, string TagLine)? ParseRiotId(string riotId)
        {
            var normalized = (riotId ?? "").Trim();
            var separatorIndex = normalized.LastIndexOf('#');
            if (separatorIndex <= 0 || separatorIndex == normalized.Length - 1)
                return null;

            var gameName = normalized[..separatorIndex].Trim();
            var tagLine = normalized[(separatorIndex + 1)..].Trim();

            if (gameName.Length < 3 || gameName.Length > 16 || tagLine.Length < 3 || tagLine.Length > 5)
                return null;

            return (gameName, tagLine);
        }

        private static string GetRegionalRoute(string platformRoute)
        {
            var normalized = (platformRoute ?? "br1").Trim().ToLowerInvariant();

            return normalized switch
            {
                "br1" or "la1" or "la2" or "na1" => "americas",
                "eun1" or "euw1" or "me1" or "ru" or "tr1" => "europe",
                "jp1" or "kr" => "asia",
                "oc1" or "ph2" or "sg2" or "th2" or "tw2" or "vn2" => "sea",
                _ => "americas"
            };
        }

        }
}
