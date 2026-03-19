using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.UserServices
{
    public interface IUserInterface
    {
        Task<Response<string>> UpdateUserAsync(UserDto user, Guid id);

        
        Task<Response<string>> UpdatePlayerFromRiotAsync(Guid userId, string summonerName, string region = "br1");
    }
}
