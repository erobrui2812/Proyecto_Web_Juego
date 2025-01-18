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
            // Validación del DTO
            if (dto == null)
            {
                Console.WriteLine("Login failed: DTO is null");
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.NicknameMail))
            {
                Console.WriteLine("Login failed: Missing NicknameMail");
                return BadRequest("Nickname or Email is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                Console.WriteLine("Login failed: Missing Password");
                return BadRequest("Password is required.");
            }

            try
            {
                Console.WriteLine($"Login request: NicknameMail={dto.NicknameMail}, Password=****");

                // Buscar usuario por Nickname o Email
                var user = _context.Users.FirstOrDefault(u =>
                    u.Email == dto.NicknameMail || u.Nickname.ToLower() == dto.NicknameMail.ToLower());

                if (user == null)
                {
                    Console.WriteLine($"User not found for NicknameMail={dto.NicknameMail}");
                    return Unauthorized("Invalid credentials");
                }

                // Verificar contraseña
                if (!_authService.VerifyPassword(dto.Password, user.PasswordHash))
                {
                    Console.WriteLine($"Invalid password for user {user.Nickname}");
                    return Unauthorized("Invalid credentials");
                }

                // Generar token JWT
                var token = _authService.GenerateJwtToken(user);
                Console.WriteLine($"Generated token for user {user.Nickname}: {token}");

                // Devolver respuesta exitosa
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in login: {ex.Message} - {ex.StackTrace}");
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
            var userIdInt = _authService.GetUserIdFromTokenAsInt(Request.Headers["Authorization"].ToString());
            if (!userIdInt.HasValue)
                return Unauthorized("Invalid or missing token");

            var user = await _context.Users
                .Where(u => u.Id == userIdInt.Value)
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
