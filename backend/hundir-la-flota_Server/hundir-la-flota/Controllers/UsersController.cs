using Microsoft.AspNetCore.Mvc;

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
            var user = _context.Users.FirstOrDefault(u =>
                u.Email == dto.NicknameMail || u.Nickname.ToLower() == dto.NicknameMail.ToLower());

            if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = _authService.GenerateJwtToken(user);
            return Ok(new { Token = token });
        }


    }
}
