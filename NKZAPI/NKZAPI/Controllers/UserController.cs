
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
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
        [HttpPost("CreateUsers")]
        public async Task<ActionResult<User>> CreateUserAsync([FromBody] UserDto user)
        {
            try
            {
                // Mapeia manualmente UserDto para User
                var userModel = new User
                {
                    Id = user.Id,
                    CreatedAt = user.CreatedAt,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    Player = user.Player
                };

                User createdUser = await _userServices.AddUserAsync(userModel);
                return createdUser;
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

    }
}
