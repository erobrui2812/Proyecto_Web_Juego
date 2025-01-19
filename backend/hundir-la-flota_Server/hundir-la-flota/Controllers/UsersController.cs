using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

namespace hundir_la_flota.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            var result = await _userService.RegisterUserAsync(dto);
            if (!result.Success) return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginDTO dto)
        {
            var result = _userService.AuthenticateUser(dto);
            if (!result.Success) return Unauthorized(result.Message);

            return Ok(new { Token = result.Data });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetUserDetail()
        {
            var result = await _userService.GetUserDetailAsync(Request.Headers["Authorization"].ToString());
            if (!result.Success) return Unauthorized(result.Message);

            return Ok(result.Data);
        }
    }
}
