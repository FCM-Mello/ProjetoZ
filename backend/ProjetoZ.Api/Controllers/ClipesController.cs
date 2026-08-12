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
[Route("api/clipes")]
[Authorize]
public class ClipesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly YoutubeService _youtubeService;

    public ClipesController(ApplicationDbContext context, YoutubeService youtubeService)
    {
        _context = context;
        _youtubeService = youtubeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var meuId = MeuId();

        if (meuId == null)
            return Unauthorized();

        var clipes = await _context.Clipes.ToListAsync();
        var curtidas = await _context.ClipeCurtidas.ToListAsync();

        var autoresIds = clipes.Select(c => c.UserId).Distinct().ToList();
        var autores = await _context.Users
            .Where(u => autoresIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var curtidasPorClipe = curtidas
            .GroupBy(c => c.ClipeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var meusLikes = curtidas
            .Where(c => c.UserId == meuId)
            .Select(c => c.ClipeId)
            .ToHashSet();

        var dtos = clipes
            .Select(c => new ClipeDto
            {
                Id = c.Id,
                Titulo = c.Titulo,
                Url = c.Url,
                AutorNome = autores.TryGetValue(c.UserId, out var autor) ? (autor.Profile?.Name ?? "Usuário") : "Usuário",
                AutorAvatar = autores.TryGetValue(c.UserId, out var autor2) ? (autor2.Profile?.Avatar ?? "") : "",
                AutorSouEu = c.UserId == meuId,
                Curtidas = curtidasPorClipe.GetValueOrDefault(c.Id, 0),
                JaCurti = meusLikes.Contains(c.Id),
                CriadoEm = c.CriadoEm
            })
            .OrderByDescending(c => c.Curtidas)
            .ThenByDescending(c => c.CriadoEm)
            .ToList();

        var config = await _context.ClipeConfigs.FirstOrDefaultAsync();

        ClipeVencedorDto? ultimoVencedor = null;

        if (config?.UltimoVencedorTitulo != null)
        {
            ultimoVencedor = new ClipeVencedorDto
            {
                Titulo = config.UltimoVencedorTitulo,
                Url = config.UltimoVencedorUrl ?? "",
                AutorNome = config.UltimoVencedorAutorNome ?? "Usuário",
                AutorAvatar = config.UltimoVencedorAutorAvatar ?? "",
                Curtidas = config.UltimoVencedorCurtidas ?? 0,
                FechadoEm = config.UltimoVencedorFechadoEm ?? config.UltimoFechamento
            };
        }

        return Ok(new ClipesResponseDto
        {
            ProximoFechamento = Semana.ProximoFechamentoUtc(),
            Clipes = dtos,
            UltimoVencedor = ultimoVencedor
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateClipeRequest request)
    {
        var meuId = MeuId();

        if (meuId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Titulo))
            return BadRequest("Título é obrigatório.");

        var usuario = await _context.Users.FindAsync(meuId.Value);

        if (usuario == null)
            return Unauthorized();

        if (string.IsNullOrEmpty(usuario.YoutubeChannelId))
            return BadRequest("Vincule seu canal do YouTube antes de postar um clipe.");

        var videoId = YoutubeService.ExtrairVideoId(request.Url ?? "");

        if (videoId == null)
            return BadRequest("Informe um link válido de vídeo do YouTube.");

        var infoVideo = await _youtubeService.ObterInfoDoVideoAsync(videoId);

        if (infoVideo == null)
            return BadRequest("Não foi possível encontrar esse vídeo no YouTube.");

        if (infoVideo.Value.ChannelId != usuario.YoutubeChannelId)
            return BadRequest("Esse vídeo não pertence ao canal do YouTube vinculado à sua conta.");

        if (infoVideo.Value.PublicadoEm < Semana.InicioSemanaAtualUtc())
            return BadRequest("Esse vídeo é de antes do início da semana atual — só valem clipes publicados durante a semana do ranking.");

        if (!infoVideo.Value.Titulo.Contains("arkz", StringComparison.OrdinalIgnoreCase))
            return BadRequest("O título do vídeo no YouTube precisa mencionar \"ArkZ\".");

        var clipe = new Clipe
        {
            Id = Guid.NewGuid(),
            UserId = meuId.Value,
            Titulo = request.Titulo.Trim(),
            Url = (request.Url ?? "").Trim(),
        };

        _context.Clipes.Add(clipe);
        await _context.SaveChangesAsync();

        return Ok(clipe);
    }

    [HttpPost("{id}/curtir")]
    public async Task<IActionResult> Curtir(Guid id)
    {
        var meuId = MeuId();

        if (meuId == null)
            return Unauthorized();

        var clipe = await _context.Clipes.FindAsync(id);

        if (clipe == null)
            return NotFound();

        if (clipe.UserId == meuId)
            return BadRequest("Você não pode curtir seu próprio clipe.");

        var jaCurtiu = await _context.ClipeCurtidas
            .AnyAsync(c => c.ClipeId == id && c.UserId == meuId);

        if (!jaCurtiu)
        {
            _context.ClipeCurtidas.Add(new ClipeCurtida
            {
                Id = Guid.NewGuid(),
                ClipeId = id,
                UserId = meuId.Value
            });

            await _context.SaveChangesAsync();
        }

        var total = await _context.ClipeCurtidas.CountAsync(c => c.ClipeId == id);

        return Ok(new { curtidas = total });
    }

    private Guid? MeuId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId != null && Guid.TryParse(userId, out var id) ? id : null;
    }
}
