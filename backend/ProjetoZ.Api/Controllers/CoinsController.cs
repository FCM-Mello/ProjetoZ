using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/coins")]
public class CoinsController : ControllerBase
{
    private static readonly Dictionary<string, CoinPackage> Pacotes = new()
    {
        ["bronze"] = new CoinPackage("bronze", "Pacote Bronze", 500, 9.90m),
        ["prata"] = new CoinPackage("prata", "Pacote Prata", 1200, 19.90m),
        ["ouro"] = new CoinPackage("ouro", "Pacote Ouro", 3000, 39.90m),
        ["platina"] = new CoinPackage("platina", "Pacote Platina", 7000, 79.90m),
    };

    private readonly ApplicationDbContext _context;
    private readonly MercadoPagoService _mercadoPago;
    private readonly IConfiguration _configuration;

    public CoinsController(
        ApplicationDbContext context,
        MercadoPagoService mercadoPago,
        IConfiguration configuration)
    {
        _context = context;
        _mercadoPago = mercadoPago;
        _configuration = configuration;
    }

    [HttpGet("pacotes")]
    public IActionResult GetPacotes()
    {
        return Ok(Pacotes.Values);
    }

    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout(PurchaseCoinsRequest request)
    {
        if (!Pacotes.TryGetValue(request.PackageId, out var pacote))
            return BadRequest("Pacote inválido.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var pedido = new PedidoCoins
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userId),
            PackageId = pacote.Id,
            Coins = pacote.Coins,
            Valor = pacote.PrecoReais,
            Status = "pending"
        };

        _context.PedidosCoins.Add(pedido);
        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["App:FrontendUrl"]
            ?? throw new InvalidOperationException("App:FrontendUrl não configurado.");

        var backendUrl = _configuration["App:BackendUrl"]
            ?? throw new InvalidOperationException("App:BackendUrl não configurado.");

        var preferencia = await _mercadoPago.CriarPreferenciaAsync(
            pedido.Id,
            pacote.Nome,
            pacote.PrecoReais,
            frontendUrl,
            $"{backendUrl}/api/coins/webhook");

        return Ok(new { redirectUrl = preferencia.InitPoint });
    }

    // Chamado pelo Mercado Pago quando o status de um pagamento muda.
    // Precisa responder 200 rapidamente; a validação real acontece consultando
    // a API de pagamentos do MP (nunca confiar apenas no corpo da notificação).
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(
        [FromQuery(Name = "id")] string? id,
        [FromQuery(Name = "data.id")] string? dataId)
    {
        var paymentId = dataId ?? id;

        if (string.IsNullOrEmpty(paymentId))
            return Ok();

        if (!ValidarAssinaturaWebhook(Request, paymentId))
            return Unauthorized();

        var pagamento = await _mercadoPago.ConsultarPagamentoAsync(paymentId);

        if (pagamento?.ExternalReference == null)
            return Ok();

        if (!Guid.TryParse(pagamento.ExternalReference, out var pedidoId))
            return Ok();

        var pedido = await _context.PedidosCoins.FindAsync(pedidoId);

        if (pedido == null || pedido.Status == "approved")
            return Ok();

        if (pagamento.Status == "approved")
        {
            var user = await _context.Users.FindAsync(pedido.UserId);

            if (user != null)
            {
                user.Coins += pedido.Coins;
                pedido.Status = "approved";
                pedido.MercadoPagoPaymentId = paymentId;

                _context.Compras.Add(new Compra
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Tipo = "coins",
                    Descricao = Pacotes.TryGetValue(pedido.PackageId, out var pacote) ? pacote.Nome : pedido.PackageId,
                    Coins = pedido.Coins,
                    ValorReais = pedido.Valor,
                });

                await _context.SaveChangesAsync();
            }
        }
        else
        {
            pedido.Status = pagamento.Status;
            pedido.MercadoPagoPaymentId = paymentId;

            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    // Ver: https://www.mercadopago.com.br/developers/pt/docs/checkout-api/webhooks#Validar-origem-da-notificacao
    // Verifique este algoritmo contra o simulador de webhooks do painel do MP
    // ao configurar o segredo real — o formato exato do manifest é sensível.
    private bool ValidarAssinaturaWebhook(HttpRequest httpRequest, string dataId)
    {
        var secret = _configuration["MercadoPago:WebhookSecret"];

        // Falha fechado: sem segredo configurado, nenhuma notificação é
        // aceita (antes isso deixava o webhook completamente aberto pra
        // qualquer POST anônimo se MercadoPago:WebhookSecret estivesse vazio).
        if (string.IsNullOrEmpty(secret))
            return false;

        if (!httpRequest.Headers.TryGetValue("x-signature", out var signatureHeader))
            return false;

        if (!httpRequest.Headers.TryGetValue("x-request-id", out var requestId))
            return false;

        string? ts = null;
        string? v1 = null;

        foreach (var part in signatureHeader.ToString().Split(','))
        {
            var kv = part.Split('=', 2);

            if (kv.Length != 2)
                continue;

            if (kv[0].Trim() == "ts") ts = kv[1].Trim();
            if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
        }

        if (ts == null || v1 == null)
            return false;

        var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";

        var hash = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest))
        ).ToLowerInvariant();

        return hash == v1;
    }

    public record CoinPackage(string Id, string Nome, int Coins, decimal PrecoReais);
}
