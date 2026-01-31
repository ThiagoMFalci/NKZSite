using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NKZAPI.Data;
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

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            User? Return = await _context.Users.FindAsync(id);
            return Return;
        }

        public async Task<User> AddUserAsync(User user)
        {
            EntityEntry<User> Return = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Return.Entity;
        }


       
    }
}
