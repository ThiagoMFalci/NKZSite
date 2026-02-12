using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.PassService;

namespace NKZAPI.Services.AuthServices
{
    public class AuthService : IAuthInterface
    {
        private readonly NKZAPIContext _context;
        private readonly IPasswordInterface _passInterface;
        public AuthService(NKZAPIContext context, IPasswordInterface passInterface)
        {
            _context = context;
            _passInterface = passInterface;
        }

        public async Task<Response<UserDto>> UserAddAsync(UserDto User)
        {
            Response<UserDto> response = new Response<UserDto>();
            try
            {
                if (!VerifyIfUserExists(User))
                {
                    response.Data = null;
                    response.Message = "User already exists";
                    response.Success = false;
                    return response;
                }

                _passInterface.CreatePassHash(User.PasswordHash, out byte[] passwordHash, out byte[] passwordSalt);
                User user = new User
                {
                    Email = User.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Player = new List<Player>()
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                response.Message = "User created successfully";

            }
            catch (Exception ex)
            {
                response.Data = null;
                response.Message = ex.Message;
                response.Success = false;
            }
            return response;
        }

        public async Task<Response<string>> Login(UserLoginDto userLogin)
        {
            Response<string> response = new Response<string>();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLogin.Email);
                if (user == null)
                {
                    response.Data = null;
                    response.Message = "User not found";
                    response.Success = false;
                    return response;
                }
                if (!_passInterface.VerifyPassHash(userLogin.Password, user.PasswordHash, user.PasswordSalt))
                {
                    response.Data = null;
                    response.Message = "Incorrect password";
                    response.Success = false;
                    return response;
                }
                var token = _passInterface.CreateToken(user);

                response.Message = "Login successful";
                response.Data = token;
                response.Success = true;
            }

            catch (Exception ex)
            {
                response.Data = null;
                response.Message = ex.Message;
                response.Success = false;
            }

            return response;
        }
        public bool VerifyIfUserExists(UserDto User)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == User.Email);
            if (user != null) return false;
            return true;

        }

    }

}
