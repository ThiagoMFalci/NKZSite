using NKZAPI.Data;
using System.Data.Entity;
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

        public async Task<User?> GetUserByIdAsync(int id)
        {
            User? Return = await _context.Users.FindAsync(id);
            return Return;
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            User? user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(e => e.Id == id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            return await _context.Users.Where(u => u.Role == role).ToListAsync();
        }

        public async Task<int> GetUserCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            return await _context.Users
                .Where(u => u.Username.Contains(searchTerm) || u.Email.Contains(searchTerm))
                .ToListAsync();
        }
        public async Task<List<User>> GetUsersPagedAsync(int pageNumber, int pageSize)
        {
            return await _context.Users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<List<User>> GetUsersSortedByUsernameAsync(bool ascending = true)
        {
            if (ascending)
            {
                return await _context.Users
                    .OrderBy(u => u.Username)
                    .ToListAsync();
            }
            else
            {
                return await _context.Users
                    .OrderByDescending(u => u.Username)
                    .ToListAsync();
            }
        }
        public async Task<List<User>> GetUsersRegisteredAfterAsync(DateTime date)
        {
            return await _context.Users
                .Where(u => u.RegistrationDate > date)
                .ToListAsync();
        }
    }
}
