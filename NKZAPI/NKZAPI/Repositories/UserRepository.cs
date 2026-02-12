using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NKZAPI.Data;
using NKZAPI.Dtos;
using NKZAPI.Models;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace NKZAPI.Repositories
{
    public class UserRepository
    {
        private readonly NKZAPIContext _context;
        public UserRepository(NKZAPIContext context)
        {
            _context = context;
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            List<User> Return = await _context.Users.ToListAsync();
            return Return;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            User? user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            UserDto dto = new UserDto
            {
                Email = user.Email,
                PasswordHash = Convert.ToBase64String(user.PasswordHash),
                PasswordSalt = Convert.ToBase64String(user.PasswordSalt),
                Player = user.Player
            };
            return dto;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddUserAsync(User user)
        {
            EntityEntry<User> Return = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Return.Entity;
        }
        public async Task<User> UpdateUserAsync(User user)
        {
            EntityEntry<User> Return = _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Return.Entity;
        }
        public async Task DeleteUserAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

    }
}
