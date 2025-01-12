using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hundir_la_flota.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly AuthService _authService;

        public UsersController(MyDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email || u.Nickname.ToLower() == dto.Nickname.ToLower()))
                return BadRequest("Email or Nickname already in use");

            var user = new User
            {
                Nickname = dto.Nickname,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password),
                AvatarUrl = dto.AvatarUrl
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginDTO dto)
        {
            try
            {
                // Log de entrada
                Console.WriteLine($"Login request: NicknameMail={dto.NicknameMail}, Password={dto.Password}");

                var user = _context.Users.FirstOrDefault(u =>
                    u.Email == dto.NicknameMail || u.Nickname.ToLower() == dto.NicknameMail.ToLower());

                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return Unauthorized("Invalid credentials");
                }

                if (!_authService.VerifyPassword(dto.Password, user.PasswordHash))
                {
                    Console.WriteLine("Invalid password");
                    return Unauthorized("Invalid credentials");
                }

                var token = _authService.GenerateJwtToken(user);
                Console.WriteLine($"Generated token: {token}");

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in login: {ex.Message}");
                return StatusCode(500, "An error occurred during login");
            }
        }


        [HttpGet("list")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Nickname,
                    u.Email,
                    u.PasswordHash,
                    u.AvatarUrl
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetUserDetail()
        {
            var userId = _authService.GetUserIdFromToken(Request.Headers["Authorization"].ToString());

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid or missing token");

            if (!int.TryParse(userId, out var userIdInt))
                return BadRequest("Invalid user ID in token");

            var user = await _context.Users
                .Where(u => u.Id == userIdInt)
                .Select(u => new
                {
                    u.Nickname,
                    u.Email,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

    }
}
