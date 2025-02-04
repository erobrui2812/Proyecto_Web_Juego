using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class AdminService
{
    private readonly MyDbContext _context;

    public AdminService(MyDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ChangeUserRole(int userId, string newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleUserBlock(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsBlocked = !user.IsBlocked;
        await _context.SaveChangesAsync();
        return true;
    }
}
