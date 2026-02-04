
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services;
using NKZAPI.Services.AuthServices;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/auth/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly IAuthInterface _authInterface;
        public UserController(UserServices userServices, IAuthInterface authInterface)
        {
            _userServices = userServices;
            _authInterface = authInterface;
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
            var response = await _authInterface.UserAddAsync(user);
            return Ok(response);
        }
        [HttpPost("Login")]
        public async Task<ActionResult<User>> Login(UserLoginDto userLogin)
        {
            var response = await _authInterface.Login(userLogin);
            return Ok(response);
        }

    }
}
