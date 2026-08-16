using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

// Endpoints voltados pro site (autenticação por JWT, usuário logado
// consultando os próprios dados) — não confundir com os endpoints de seguro
// do GameController, que são servidor-a-servidor e autenticam por ApiKey
// compartilhada com o mod.
[ApiController]
[Route("api/seguros")]
[Authorize]
public class SegurosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SegurosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Seguros ativos (não expirados) do usuário logado, com a posição do
    // veículo quando já tiver sido sincronizada pelo mod.
    [HttpGet("meus")]
    public async Task<IActionResult> GetMeus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var seguros = await _context.Seguros
            .Where(s => s.UserId == Guid.Parse(userId) && s.ExpiraEm > DateTime.UtcNow)
            .OrderBy(s => s.CriadoEm)
            .Select(s => new SeguroAtivoDto
            {
                IdSeguro = s.Id,
                Id = s.ItemId,
                ExpiraEm = s.ExpiraEm,
                CarroId = s.CarroId,
                VeiculoNome = s.VeiculoNome,
                PosicaoGrid = s.PosicaoGrid,
                PosicaoX = s.PosicaoX,
                PosicaoZ = s.PosicaoZ,
                PosicaoAtualizadaEm = s.PosicaoAtualizadaEm
            })
            .ToListAsync();

        return Ok(seguros);
    }
}
