using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Persistence;

namespace ProjetoZ.Api.Controllers;

// Ranking global de players (K/D e KOTH completados) — dados vêm do mod via
// GameController.SincronizarKd/RegistrarKoth. Essa parte é a exibida no
// site: qualquer usuário logado pode ver, só admin pode resetar.
[ApiController]
[Route("api/ranking")]
[Authorize]
public class RankingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RankingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRanking()
    {
        // Seleciona campos escalares do Profile em vez do objeto inteiro —
        // Profile é um owned type, e projetar ele sozinho (sem o User dono
        // junto) numa query rastreada faz o EF Core lançar em runtime.
        var brutos = await (
            from r in _context.PlayerRankings
            join u in _context.Users on r.UserId equals u.Id
            where u.Profile != null
            select new
            {
                SteamId = u.Profile!.SteamId,
                Nome = u.Profile.Name,
                Avatar = u.Profile.Avatar,
                r.Kills,
                r.Deaths,
                r.KothCompletados,
            }
        ).ToListAsync();

        var ranking = brutos
            .Select(b => new RankingJogadorDto
            {
                SteamId = b.SteamId ?? string.Empty,
                Nome = b.Nome ?? "Jogador",
                Avatar = b.Avatar ?? string.Empty,
                Kills = b.Kills,
                Deaths = b.Deaths,
                Kd = RankingCalculos.CalcularKd(b.Kills, b.Deaths),
                KothCompletados = b.KothCompletados,
            })
            .OrderByDescending(r => r.Kd)
            .ToList();

        return Ok(ranking);
    }

    // Reset é global — zera o ranking de todo mundo de uma vez (ex: início
    // de temporada), não por jogador individual.
    [HttpDelete]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ResetarRanking()
    {
        await _context.PlayerRankings.ExecuteDeleteAsync();

        return NoContent();
    }
}
