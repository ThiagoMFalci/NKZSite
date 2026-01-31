
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Models;
using NKZAPI.Services;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/auth/[controller]")]
    public class UserController
    {
        private readonly UserServices _userServices;
        public UserController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpGet("ListUsers")]
        public async Task<ActionResult<List<User>>> ListUsersAsync()
        {
            List<User> users = await _userServices.GetAllUsersAsync();
            return users;
        }
        [HttpPost]
        public async Task<ActionResult<User>> CreateUserAsync([FromBody] User user)
        {
            User createdUser = await _userServices.AddUserAsync(user);
            return createdUser;
        }

    }
}
