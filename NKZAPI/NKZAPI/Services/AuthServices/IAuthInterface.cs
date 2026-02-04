using NKZAPI.Dtos;
using NKZAPI.Models;

namespace NKZAPI.Services.AuthServices
{
    public interface IAuthInterface
    {
        Task<Response<UserDto>> UserAddAsync(UserDto User);
        Task<Response<string>> Login(UserLoginDto userLogin);
    }
}
