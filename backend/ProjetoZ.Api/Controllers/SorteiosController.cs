using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/sorteios")]
[Authorize]
public class SorteiosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SorteiosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var meuId = Guid.Parse(userId);

        var sorteios = await _context.Sorteios
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();

        var todosProdutoIds = sorteios
            .SelectMany(s => s.PremioProdutoIds)
            .Distinct()
            .ToList();

        var produtosPorId = await _context.Products
            .Where(p => todosProdutoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var participacoes = await _context.SorteioParticipantes
            .Where(p => p.UserId == meuId)
            .Select(p => p.SorteioId)
            .ToListAsync();

        var participantesPorSorteio = await _context.SorteioParticipantes
            .GroupBy(p => p.SorteioId)
            .Select(g => new { SorteioId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.SorteioId, g => g.Total);

        var vencedoresIds = sorteios
            .Where(s => s.VencedorUserId.HasValue)
            .Select(s => s.VencedorUserId!.Value)
            .Distinct()
            .ToList();

        var vencedoresPorId = await _context.Users
            .Where(u => vencedoresIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var resultado = sorteios.Select(s => new SorteioDto
        {
            Id = s.Id,
            Titulo = s.Titulo,
            Descricao = s.Descricao,
            PremioVipNivel = s.PremioVipNivel,
            PremioVipNivelNome = s.PremioVipNivel.HasValue ? VipTiers.NomeDoNivel(s.PremioVipNivel.Value) : null,
            PremioProdutos = s.PremioProdutoIds
                .Where(id => produtosPorId.ContainsKey(id))
                .Select(id => new SorteioProdutoDto
                {
                    Id = id,
                    Nome = produtosPorId[id].Nome,
                    Imagem = produtosPorId[id].Imagem
                })
                .ToList(),
            Status = s.Status,
            TotalParticipantes = participantesPorSorteio.GetValueOrDefault(s.Id, 0),
            JaParticipando = participacoes.Contains(s.Id),
            VencedorNome = s.VencedorUserId.HasValue && vencedoresPorId.TryGetValue(s.VencedorUserId.Value, out var vencedor)
                ? (vencedor.Profile?.Name ?? "Usuário")
                : null,
            CriadoEm = s.CriadoEm,
            SorteadoEm = s.SorteadoEm
        });

        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create(CreateSorteioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return BadRequest("Título é obrigatório.");

        var temPremioVip = request.PremioVipNivel is >= 1 and <= 3;
        var temPremioProdutos = request.PremioProdutoIds.Count > 0;

        if (!temPremioVip && !temPremioProdutos)
            return BadRequest("Escolha ao menos um prêmio: um nível de VIP ou um ou mais produtos.");

        var sorteio = new Sorteio
        {
            Id = Guid.NewGuid(),
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            PremioVipNivel = temPremioVip ? request.PremioVipNivel : null,
            PremioProdutoIds = request.PremioProdutoIds,
            Status = "aberto"
        };

        _context.Sorteios.Add(sorteio);
        await _context.SaveChangesAsync();

        return Ok(sorteio);
    }

    [HttpPost("{id}/entrar")]
    public async Task<IActionResult> Entrar(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var meuId = Guid.Parse(userId);

        var sorteio = await _context.Sorteios.FindAsync(id);

        if (sorteio == null)
            return NotFound();

        if (sorteio.Status != "aberto")
            return BadRequest("Este sorteio já foi encerrado.");

        var jaParticipa = await _context.SorteioParticipantes
            .AnyAsync(p => p.SorteioId == id && p.UserId == meuId);

        if (jaParticipa)
            return Ok();

        _context.SorteioParticipantes.Add(new SorteioParticipante
        {
            Id = Guid.NewGuid(),
            SorteioId = id,
            UserId = meuId
        });

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/sortear")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Sortear(Guid id)
    {
        var sorteio = await _context.Sorteios.FindAsync(id);

        if (sorteio == null)
            return NotFound();

        if (sorteio.Status != "aberto")
            return BadRequest("Este sorteio já foi encerrado.");

        var participantes = await _context.SorteioParticipantes
            .Where(p => p.SorteioId == id)
            .ToListAsync();

        if (participantes.Count == 0)
            return BadRequest("Nenhum participante inscrito nesse sorteio.");

        var vencedor = participantes[Random.Shared.Next(participantes.Count)];

        var ganhador = await _context.Users.FindAsync(vencedor.UserId);

        if (ganhador == null)
            return BadRequest("Participante vencedor não encontrado.");

        if (sorteio.PremioVipNivel.HasValue)
        {
            ganhador.VipNivel = sorteio.PremioVipNivel.Value;
            ganhador.VipExpiraEm = DateTime.UtcNow.AddDays(VipTiers.DuracaoDias);
        }

        if (sorteio.PremioProdutoIds.Count > 0)
        {
            ganhador.Inventario = [.. ganhador.Inventario, .. sorteio.PremioProdutoIds];
        }

        sorteio.Status = "sorteado";
        sorteio.VencedorUserId = ganhador.Id;
        sorteio.SorteadoEm = DateTime.UtcNow;

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = ganhador.Id,
            Tipo = "sorteio",
            Descricao = sorteio.Titulo,
            Coins = 0,
        });

        await _context.SaveChangesAsync();

        return Ok(new { vencedorUserId = ganhador.Id, vencedorNome = ganhador.Profile?.Name });
    }
}
