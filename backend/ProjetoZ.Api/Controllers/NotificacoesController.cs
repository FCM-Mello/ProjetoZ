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
[Route("api/notificacoes")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificacoesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("minhas")]
    public async Task<IActionResult> GetMinhas()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var meuId = Guid.Parse(userId);
        var agora = DateTime.UtcNow;

        var visiveis = await _context.Notificacoes
            .Where(n => n.EnviarEm <= agora && n.ExpiraEm > agora)
            .Where(n => n.ParaTodos || _context.NotificacaoDestinatarios
                .Any(d => d.NotificacaoId == n.Id && d.UserId == meuId))
            .OrderByDescending(n => n.EnviarEm)
            .ToListAsync();

        var lidas = await _context.NotificacaoLeituras
            .Where(l => l.UserId == meuId && visiveis.Select(n => n.Id).Contains(l.NotificacaoId))
            .Select(l => l.NotificacaoId)
            .ToListAsync();

        var lidasSet = lidas.ToHashSet();

        return Ok(visiveis.Select(n => new NotificacaoDto
        {
            Id = n.Id,
            Titulo = n.Titulo,
            Mensagem = n.Mensagem,
            Nivel = n.Nivel,
            EnviarEm = n.EnviarEm,
            Lida = lidasSet.Contains(n.Id)
        }));
    }

    [HttpPost("{id}/lida")]
    public async Task<IActionResult> MarcarLida(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var meuId = Guid.Parse(userId);

        var jaLida = await _context.NotificacaoLeituras
            .AnyAsync(l => l.NotificacaoId == id && l.UserId == meuId);

        if (!jaLida)
        {
            _context.NotificacaoLeituras.Add(new NotificacaoLeitura
            {
                NotificacaoId = id,
                UserId = meuId
            });

            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost("lidas")]
    public async Task<IActionResult> MarcarTodasLidas()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var meuId = Guid.Parse(userId);
        var agora = DateTime.UtcNow;

        var visiveisIds = await _context.Notificacoes
            .Where(n => n.EnviarEm <= agora && n.ExpiraEm > agora)
            .Where(n => n.ParaTodos || _context.NotificacaoDestinatarios
                .Any(d => d.NotificacaoId == n.Id && d.UserId == meuId))
            .Select(n => n.Id)
            .ToListAsync();

        var jaLidasIds = await _context.NotificacaoLeituras
            .Where(l => l.UserId == meuId && visiveisIds.Contains(l.NotificacaoId))
            .Select(l => l.NotificacaoId)
            .ToListAsync();

        var faltamMarcar = visiveisIds.Except(jaLidasIds);

        foreach (var notificacaoId in faltamMarcar)
        {
            _context.NotificacaoLeituras.Add(new NotificacaoLeitura
            {
                NotificacaoId = notificacaoId,
                UserId = meuId
            });
        }

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Criar(CriarNotificacaoRequest request)
    {
        if (!NotificacaoNiveis.NivelValido(request.Nivel))
            return BadRequest("Nível inválido — use verde, amarelo ou vermelho.");

        var destinatarioIds = new List<Guid>();

        if (!request.ParaTodos)
        {
            var pedidos = (request.DestinatarioUserIds ?? []).Distinct().ToList();

            if (pedidos.Count == 0)
                return BadRequest("Informe ao menos um destinatário, ou marque para enviar a todos.");

            destinatarioIds = await _context.Users
                .Where(u => pedidos.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (destinatarioIds.Count == 0)
                return BadRequest("Nenhum dos destinatários informados existe.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var agora = DateTime.UtcNow;
        var enviarEm = request.EnviarEm ?? agora;

        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            Titulo = request.Titulo.Trim(),
            Mensagem = request.Mensagem.Trim(),
            Nivel = request.Nivel.ToLowerInvariant(),
            CriadoEm = agora,
            CriadoPorUserId = Guid.Parse(userId),
            EnviarEm = enviarEm,
            ExpiraEm = enviarEm.AddDays(NotificacaoNiveis.DiasAteExpirar),
            ParaTodos = request.ParaTodos
        };

        _context.Notificacoes.Add(notificacao);

        foreach (var destinatarioId in destinatarioIds)
        {
            _context.NotificacaoDestinatarios.Add(new NotificacaoDestinatario
            {
                Id = Guid.NewGuid(),
                NotificacaoId = notificacao.Id,
                UserId = destinatarioId
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { id = notificacao.Id });
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetTodas()
    {
        var notificacoes = await _context.Notificacoes
            .OrderByDescending(n => n.CriadoEm)
            .Take(200)
            .ToListAsync();

        var ids = notificacoes.Select(n => n.Id).ToList();

        var totalUsuarios = await _context.Users.CountAsync();

        var destinatariosPorNotificacao = await _context.NotificacaoDestinatarios
            .Where(d => ids.Contains(d.NotificacaoId))
            .GroupBy(d => d.NotificacaoId)
            .Select(g => new { NotificacaoId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.NotificacaoId, g => g.Total);

        var leiturasPorNotificacao = await _context.NotificacaoLeituras
            .Where(l => ids.Contains(l.NotificacaoId))
            .GroupBy(l => l.NotificacaoId)
            .Select(g => new { NotificacaoId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.NotificacaoId, g => g.Total);

        return Ok(notificacoes.Select(n => new NotificacaoAdminDto
        {
            Id = n.Id,
            Titulo = n.Titulo,
            Mensagem = n.Mensagem,
            Nivel = n.Nivel,
            CriadoEm = n.CriadoEm,
            EnviarEm = n.EnviarEm,
            ExpiraEm = n.ExpiraEm,
            ParaTodos = n.ParaTodos,
            TotalDestinatarios = n.ParaTodos ? totalUsuarios : destinatariosPorNotificacao.GetValueOrDefault(n.Id, 0),
            TotalLeituras = leiturasPorNotificacao.GetValueOrDefault(n.Id, 0)
        }));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var notificacao = await _context.Notificacoes.FindAsync(id);

        if (notificacao == null)
            return NotFound();

        _context.NotificacaoDestinatarios.RemoveRange(
            _context.NotificacaoDestinatarios.Where(d => d.NotificacaoId == id));

        _context.NotificacaoLeituras.RemoveRange(
            _context.NotificacaoLeituras.Where(l => l.NotificacaoId == id));

        _context.Notificacoes.Remove(notificacao);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
