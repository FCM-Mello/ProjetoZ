using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/vip")]
public class VipController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VipController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("niveis")]
    public IActionResult GetNiveis()
    {
        var niveis = VipTiers.Nomes
            .OrderBy(kv => kv.Key)
            .Select(kv => new
            {
                nivel = kv.Key,
                nome = kv.Value,
                duracaoDias = VipTiers.DuracaoDias,
                preco = VipTiers.Precos[kv.Key]
            });

        return Ok(niveis);
    }

    [HttpPost("comprar/{nivel}")]
    [Authorize]
    public async Task<IActionResult> Comprar(int nivel)
    {
        if (!VipTiers.NivelValido(nivel))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));

        if (user == null)
            return Unauthorized();

        var preco = VipTiers.Precos[nivel];

        if (user.Coins < preco)
            return BadRequest("Saldo de Az Coins insuficiente.");

        user.Coins -= preco;
        user.VipNivel = nivel;
        user.VipExpiraEm = DateTime.UtcNow.AddDays(VipTiers.DuracaoDias);

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Tipo = "vip",
            Descricao = $"VIP {VipTiers.NomeDoNivel(nivel)}",
            Coins = preco,
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            coins = user.Coins,
            vipNivel = user.VipNivel,
            vipNivelNome = VipTiers.NomeDoNivel(user.VipNivel),
            vipExpiraEm = user.VipExpiraEm
        });
    }
}
