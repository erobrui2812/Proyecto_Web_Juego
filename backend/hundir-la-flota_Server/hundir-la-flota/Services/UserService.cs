using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;

namespace hundir_la_flota.Services
{
    public interface IUserService
    {
        Task<ServiceResponse<string>> RegisterUserAsync(UserRegisterDTO dto);
        ServiceResponse<string> AuthenticateUser(UserLoginDTO dto);
        Task<ServiceResponse<List<UserListDTO>>> GetAllUsersAsync();
        Task<ServiceResponse<object>> GetUserDetailAsync(string authorizationHeader);
    }

    public class UserService : IUserService
    {
        private readonly MyDbContext _context;
        private readonly IAuthService _authService;

        public UserService(MyDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<ServiceResponse<string>> RegisterUserAsync(UserRegisterDTO dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email || u.Nickname.ToLower() == dto.Nickname.ToLower()))
                return new ServiceResponse<string> { Success = false, Message = "Email or Nickname already in use" };

            var user = new User
            {
                Nickname = dto.Nickname,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password),
                AvatarUrl = dto.AvatarUrl
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new ServiceResponse<string> { Success = true, Message = "User registered successfully" };
        }

        public ServiceResponse<string> AuthenticateUser(UserLoginDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NicknameMail) || string.IsNullOrWhiteSpace(dto.Password))
                return new ServiceResponse<string> { Success = false, Message = "Invalid credentials" };

            var user = _context.Users.FirstOrDefault(u =>
                u.Email == dto.NicknameMail || u.Nickname.ToLower() == dto.NicknameMail.ToLower());

            if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
                return new ServiceResponse<string> { Success = false, Message = "Invalid credentials" };

            var token = _authService.GenerateJwtToken(user);
            return new ServiceResponse<string> { Success = true, Data = token };
        }

        public async Task<ServiceResponse<List<UserListDTO>>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new UserListDTO
                {
                    Id = u.Id,
                    Nickname = u.Nickname,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            return new ServiceResponse<List<UserListDTO>> { Success = true, Data = users };
        }

        public async Task<ServiceResponse<object>> GetUserDetailAsync(string authorizationHeader)
        {
            var userIdInt = _authService.GetUserIdFromToken(authorizationHeader);
            if (!userIdInt.HasValue)
                return new ServiceResponse<object> { Success = false, Message = "Invalid or missing token" };

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
                return new ServiceResponse<object> { Success = false, Message = "User not found" };

            return new ServiceResponse<object> { Success = true, Data = user };
        }
    }
}
