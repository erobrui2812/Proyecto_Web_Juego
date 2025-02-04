using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using hundir_la_flota.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPut("change-role/{userId}")]
    public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] string newRole)
    {
        var result = await _adminService.ChangeUserRole(userId, newRole);
        if (!result) return BadRequest("No se pudo cambiar el rol.");
        return Ok("Rol cambiado exitosamente.");
    }

    [HttpPut("block/{userId}")]
    public async Task<IActionResult> ToggleUserBlock(int userId)
    {
        var result = await _adminService.ToggleUserBlock(userId);
        if (!result) return BadRequest("No se pudo modificar el estado de bloqueo.");
        return Ok("Estado de bloqueo actualizado.");
    }
}
