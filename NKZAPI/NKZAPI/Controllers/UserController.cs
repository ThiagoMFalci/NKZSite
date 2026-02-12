using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NKZAPI.Dtos;
using NKZAPI.Models;
using NKZAPI.Services.AuthServices;
using NKZAPI.Services.UserServices;

namespace NKZAPI.Controllers
{
    [ApiController]
    [Route("api/auth/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly IAuthInterface _authInterface;
        private readonly IUserInterface _userInterface;
        public UserController(UserServices userServices, IAuthInterface authInterface, IUserInterface userInterface)
        {
            _userServices = userServices;
            _authInterface = authInterface;
            _userInterface = userInterface;
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
        [HttpPut("UpdateUsers")]
        public async Task<ActionResult<UserDto>> UpdateUsers([FromBody] UserDto user, Guid id)
        {
            var response = await _userInterface.UpdateUserAsync(user, id);
            return Ok(response);
        }
        [HttpDelete("DeleteUsers")]
        public async Task<ActionResult> DeleteUsers(Guid id)
        {
            var user = await _userServices.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            var i = await _userServices.DeleteUserAsync(id);
            return Ok(i);
        }


    }
}
