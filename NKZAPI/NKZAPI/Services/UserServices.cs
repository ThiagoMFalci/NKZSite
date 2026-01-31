using Microsoft.EntityFrameworkCore.ChangeTracking;
using NKZAPI.Models;
using NKZAPI.Repositories;

namespace NKZAPI.Services
{
    public class UserServices
    {
        private readonly UserRepository _userRepository;
        public UserServices(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            List<User> Return = await _userRepository.GetAllUsersAsync();
            return Return;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            User? Return = await _userRepository.GetUserByIdAsync(id);
            return Return;
        }

        public async Task<User> AddUserAsync(User user)
        {
            User Return = await _userRepository.AddUserAsync(user);

            return Return;
        }
    }
}
