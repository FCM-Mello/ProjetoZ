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
[Route("api/admin")]
[Authorize(Policy = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> GetUsuarios([FromQuery] string? busca)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            query = query.Where(u =>
                (u.Profile != null && u.Profile.Name != null && u.Profile.Name.ToLower().Contains(termo)) ||
                (u.Profile != null && u.Profile.SteamId != null && u.Profile.SteamId.Contains(termo)));
        }

        var usuarios = await query
            .OrderByDescending(u => u.UltimoLogin)
            .Take(200)
            .ToListAsync();

        return Ok(usuarios.Select(MapDto));
    }

    [HttpGet("usuarios/{id}")]
    public async Task<IActionResult> GetUsuario(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        return Ok(await MapDetalheDto(user));
    }

    [HttpPost("usuarios/{id}/coins")]
    public async Task<IActionResult> AjustarCoins(Guid id, AjustarCoinsRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.Coins = Math.Max(0, user.Coins + request.Delta);

        await _context.SaveChangesAsync();

        return Ok(new { coins = user.Coins });
    }

    [HttpPost("usuarios/{id}/coins/zerar")]
    public async Task<IActionResult> ZerarCoins(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.Coins = 0;

        await _context.SaveChangesAsync();

        return Ok(new { coins = user.Coins });
    }

    [HttpPost("usuarios/{id}/vip")]
    public async Task<IActionResult> DefinirVip(Guid id, DefinirVipRequest request)
    {
        if (!VipTiers.NivelValido(request.Nivel))
            return BadRequest("Nível de VIP inválido.");

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.VipNivel = request.Nivel;
        user.VipExpiraEm = DateTime.UtcNow.AddDays(VipTiers.DuracaoDias);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            vipNivel = user.VipNivel,
            vipNivelNome = VipTiers.NomeDoNivel(user.VipNivel),
            vipExpiraEm = user.VipExpiraEm
        });
    }

    [HttpDelete("usuarios/{id}/vip")]
    public async Task<IActionResult> RemoverVip(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.VipNivel = 0;
        user.VipExpiraEm = null;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("usuarios/{id}/inventario")]
    public async Task<IActionResult> AdicionarInventario(Guid id, AdicionarInventarioRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        var produto = await _context.Products.FindAsync(request.ProdutoId);

        if (produto == null)
            return NotFound("Produto não encontrado.");

        user.Inventario = [.. user.Inventario, produto.Id];

        await _context.SaveChangesAsync();

        return Ok(await MapDetalheDto(user));
    }

    [HttpDelete("usuarios/{id}/inventario/{produtoId}")]
    public async Task<IActionResult> RemoverInventario(Guid id, Guid produtoId)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        var itens = user.Inventario.ToList();

        if (!itens.Remove(produtoId))
            return NotFound("Usuário não possui esse item no inventário.");

        user.Inventario = itens;

        await _context.SaveChangesAsync();

        return Ok(await MapDetalheDto(user));
    }

    [HttpPost("usuarios/{id}/admin")]
    public async Task<IActionResult> TornarAdmin(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.IsAdmin = true;

        await _context.SaveChangesAsync();

        return Ok(MapDto(user));
    }

    // SteamID do dono do site — nunca pode perder o admin por aqui, nem por
    // outro admin (proteção contra erro humano ou uso indevido do painel).
    private const string SuperAdminSteamId = "76561198886359962";

    // Um admin também nunca pode remover o próprio acesso — evita que
    // alguém se tranque fora do painel (mesmo por engano).
    [HttpDelete("usuarios/{id}/admin")]
    public async Task<IActionResult> RemoverAdmin(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        if (user.Profile?.SteamId == SuperAdminSteamId)
            return BadRequest("Esse usuário não pode perder o acesso de admin.");

        var meuId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (meuId != null && Guid.TryParse(meuId, out var meuIdGuid) && meuIdGuid == id)
            return BadRequest("Você não pode remover seu próprio acesso de admin.");

        user.IsAdmin = false;

        await _context.SaveChangesAsync();

        return Ok(MapDto(user));
    }

    // Mesmas duas proteções do RemoverAdmin acima — impede um admin de se
    // trancar fora da própria conta e protege o dono do site de ser banido
    // por outro admin (erro humano ou uso indevido do painel).
    [HttpPost("usuarios/{id}/banir")]
    public async Task<IActionResult> Banir(Guid id, BanirRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        if (user.Profile?.SteamId == SuperAdminSteamId)
            return BadRequest("Esse usuário não pode ser banido.");

        var meuId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (meuId != null && Guid.TryParse(meuId, out var meuIdGuid) && meuIdGuid == id)
            return BadRequest("Você não pode banir sua própria conta.");

        user.Banido = true;
        user.BanidoMotivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim();
        user.BanidoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapDto(user));
    }

    [HttpDelete("usuarios/{id}/banir")]
    public async Task<IActionResult> RemoverBan(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.Banido = false;
        user.BanidoMotivo = null;
        user.BanidoEm = null;

        await _context.SaveChangesAsync();

        return Ok(MapDto(user));
    }

    private static AdminUsuarioDto MapDto(User user)
    {
        var vipNivel = VipTiers.NivelEfetivo(user.VipNivel, user.VipExpiraEm);

        return new AdminUsuarioDto
        {
            Id = user.Id,
            SteamId = user.Profile?.SteamId ?? string.Empty,
            Nome = user.Profile?.Name ?? "Usuário",
            Avatar = user.Profile?.Avatar ?? string.Empty,
            Coins = user.Coins,
            VipNivel = vipNivel,
            VipNivelNome = vipNivel > 0 ? VipTiers.NomeDoNivel(vipNivel) : null,
            VipExpiraEm = vipNivel > 0 ? user.VipExpiraEm : null,
            IsAdmin = user.IsAdmin,
            Banido = user.Banido,
            BanidoMotivo = user.BanidoMotivo
        };
    }

    private async Task<AdminUsuarioDetalheDto> MapDetalheDto(User user)
    {
        var idsUnicos = user.Inventario.Distinct().ToList();

        var produtosPorId = await _context.Products
            .Where(p => idsUnicos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var inventario = user.Inventario
            .Where(produtoId => produtosPorId.ContainsKey(produtoId))
            .GroupBy(produtoId => produtoId)
            .Select(g => new PlayerInventoryItemDto
            {
                ProdutoId = g.Key,
                Nome = produtosPorId[g.Key].Nome,
                Quantidade = g.Count()
            })
            .ToList();

        var seguros = await _context.Seguros
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CriadoEm)
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

        var compras = await _context.Compras
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.CriadoEm)
            .Take(20)
            .Select(c => new AdminCompraDto
            {
                Tipo = c.Tipo,
                Descricao = c.Descricao,
                Coins = c.Coins,
                ValorReais = c.ValorReais,
                CriadoEm = c.CriadoEm
            })
            .ToListAsync();

        var dto = MapDto(user);

        return new AdminUsuarioDetalheDto
        {
            Id = dto.Id,
            SteamId = dto.SteamId,
            Nome = dto.Nome,
            Avatar = dto.Avatar,
            Coins = dto.Coins,
            VipNivel = dto.VipNivel,
            VipNivelNome = dto.VipNivelNome,
            VipExpiraEm = dto.VipExpiraEm,
            IsAdmin = dto.IsAdmin,
            Banido = dto.Banido,
            BanidoMotivo = dto.BanidoMotivo,
            Inventario = inventario,
            Seguros = seguros,
            Compras = compras
        };
    }
}
