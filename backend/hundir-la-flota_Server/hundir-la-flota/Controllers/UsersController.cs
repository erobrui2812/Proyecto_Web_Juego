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
        public async Task<IActionResult> Register([FromForm] UserRegisterDTO dto, IFormFile avatar)
        {
            if (dto == null)
                return BadRequest("Los datos de registro no pueden estar vacíos.");

            var result = await _userService.RegisterUserAsync(dto, avatar);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginDTO dto)
        {
            if (dto == null)
                return BadRequest("Los datos de inicio de sesión no pueden estar vacíos.");

            var result = _userService.AuthenticateUser(dto);
            if (!result.Success)
                return Unauthorized(new { success = false, message = result.Message });

            return Ok(new { success = true, token = result.Data });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            if (!result.Success)
                return StatusCode(500, new { success = false, message = "No se pudieron obtener los usuarios." });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetUserDetail()
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                return Unauthorized(new { success = false, message = "El token de autorización es obligatorio." });

            var result = await _userService.GetUserDetailAsync(authorizationHeader);
            if (!result.Success)
                return Unauthorized(new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpGet("perfil/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El ID del usuario debe ser mayor a 0." });

            var result = await _userService.GetProfileByIdAsync(id);
            if (!result.Success)
                return NotFound(new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpGet("historial/{id}")]
        public async Task<IActionResult> GetGameHistory(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "El ID del usuario debe ser mayor a 0." });

            var result = await _userService.GetGameHistoryByIdAsync(id);
            if (!result.Success)
                return NotFound(new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data });
        }
    }
}
