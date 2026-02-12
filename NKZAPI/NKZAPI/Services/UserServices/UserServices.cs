using Microsoft.EntityFrameworkCore;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Repositories;
using NKZAPI.Services.PassService;

namespace NKZAPI.Services.UserServices
{
    public class UserServices : IUserInterface
    {
        private readonly NKZAPIContext _context;
        private readonly UserRepository _userRepository;
        private readonly IPasswordInterface _passInterface;
        public UserServices(UserRepository userRepository, NKZAPIContext NKZAPI, IPasswordInterface passInterface)
        {
            _userRepository = userRepository;
            _context = NKZAPI;
            _passInterface = passInterface; 
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }

        public async Task<User> AddUserAsync(User user)
        {
            return await _userRepository.AddUserAsync(user);
        }

        public async Task<Response<string>> UpdateUserAsync(UserDto user, Guid id)
        {
            var response = new Response<string>();

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
            {
                response.Data = null;
                response.Message = "User not found";
                response.Success = false;
                return response;
            }

            _passInterface.CreatePassHash(user.PasswordHash, out byte[] passwordHash, out byte[] passwordSalt);
            var dto = await _userRepository.GetUserByIdAsync(id);

            existingUser.Email = user.Email;
            existingUser.PasswordHash = passwordHash;
            existingUser.PasswordSalt = passwordSalt;
            if (dto?.Player != null)
            {
                existingUser.Player = dto.Player;
            }

            await _userRepository.UpdateUserAsync(existingUser);

            response.Data = id.ToString();
            response.Message = "User updated successfully";
            response.Success = true;
            return response;
        }
        public async Task<Response<string>> DeleteUserAsync(Guid id)
        {
            var response = new Response<string>();
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
            {
                response.Data = null;
                response.Message = "User not found";
                response.Success = false;
                return response;
            }
            await _userRepository.DeleteUserAsync(existingUser);
            response.Data = id.ToString();
            response.Message = "User deleted successfully";
            response.Success = true;
            return response;
        }

    }
}
