using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ComprasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMinhas()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var compras = await _context.Compras
            .Where(c => c.UserId == Guid.Parse(userId))
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

        return Ok(compras);
    }
}
