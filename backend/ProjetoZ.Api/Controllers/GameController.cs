using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace ProjetoZ.Api.Controllers;

// Endpoint servidor-a-servidor para o mod do servidor de jogo consultar o
// status de um jogador (VIP, coins, inventário) a partir do SteamID.
// Não usa JWT (o servidor de jogo não é um usuário logado no site) — em vez
// disso, valida uma chave secreta compartilhada enviada no corpo da requisição.
[ApiController]
[Route("api/game")]
public class GameController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public GameController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("player")]
    public async Task<IActionResult> GetPlayer(PlayerLookupRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var idsUnicos = user.Inventario.Distinct().ToList();

        var produtosPorId = await _context.Products
            .Where(p => idsUnicos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var inventarioEncontrado = user.Inventario
            .Where(id => produtosPorId.ContainsKey(id))
            .ToList();

        var inventario = inventarioEncontrado
            .GroupBy(id => id)
            .Select(g => new PlayerInventoryItemDto
            {
                ProdutoId = g.Key,
                Nome = produtosPorId[g.Key].Nome,
                Quantidade = g.Count()
            })
            .ToList();

        var vipNivel = VipTiers.NivelEfetivo(user.VipNivel, user.VipExpiraEm);

        return Ok(new PlayerStatusDto
        {
            SteamId = request.SteamId,
            Vip = vipNivel > 0,
            VipNivel = vipNivel,
            VipNivelNome = vipNivel > 0 ? VipTiers.NomeDoNivel(vipNivel) : null,
            VipExpiraEm = vipNivel > 0 ? user.VipExpiraEm : null,
            Coins = user.Coins,
            Inventario = inventario
        });
    }

    // Debita coins de um jogador ao comprar um item dentro da loja do mod
    // (itens que só existem no jogo, não cadastrados na tabela Products).
    // A compra é registrada em Compras (Tipo = "mod") pra aparecer também
    // no histórico do site.
    [HttpPost("comprar")]
    public async Task<IActionResult> Comprar(PlayerComprarRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.ItemId) || request.Preco <= 0)
            return BadRequest("Item inválido.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        if (user.Coins < request.Preco)
            return BadRequest("Saldo de Az Coins insuficiente.");

        user.Coins -= request.Preco;

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Tipo = "mod",
            Descricao = string.IsNullOrWhiteSpace(request.ItemNome) ? request.ItemId : request.ItemNome,
            Coins = request.Preco,
        });

        await _context.SaveChangesAsync();

        return Ok(new { coins = user.Coins });
    }

    private bool ValidarApiKey(string? providedKey)
    {
        var apiKey = _configuration["GameServer:ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(providedKey))
            return false;

        var expected = Encoding.UTF8.GetBytes(apiKey);
        var provided = Encoding.UTF8.GetBytes(providedKey);

        if (expected.Length != provided.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
