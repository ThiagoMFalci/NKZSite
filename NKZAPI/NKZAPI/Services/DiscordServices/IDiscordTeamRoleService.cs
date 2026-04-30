using NKZAPI.Models;

namespace NKZAPI.Services.DiscordServices
{
    public interface IDiscordTeamRoleService
    {
        Task EnsureLeagueCategoryAsync();
        Task SyncTeamRoleAsync(Team team);
    }
}
