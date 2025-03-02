using hundir_la_flota.Services;
using hundir_la_flota.Models;
namespace hundir_la_flota.Models.Seeder
{
    public class SeederUsers
    {
        private readonly MyDbContext _context;
        private readonly IAuthService _authService;

        public SeederUsers(MyDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task Seeder()
        {
            List<User> users = new List<User>
        {
            new User
            {
                Id = 1,
                Nickname = "TestAdmin",
                Email = "test@admin.es",
                PasswordHash = _authService.HashPassword("adminadmin"),
                Role = "admin"
            },
            new User
            {
                Id = 2,
                Nickname = "TestUser",
                Email = "test@user.es",
                PasswordHash = _authService.HashPassword("useruser"),
                Role = "user"
            }
        };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }
    }

}