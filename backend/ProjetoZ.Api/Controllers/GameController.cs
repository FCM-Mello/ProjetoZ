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

        var linhasAfetadas = await _context.Users
            .Where(u => u.Id == user.Id && u.Coins >= request.Preco)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, u => u.Coins - request.Preco));

        if (linhasAfetadas == 0)
            return BadRequest("Saldo de Az Coins insuficiente.");

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Tipo = "mod",
            Descricao = string.IsNullOrWhiteSpace(request.ItemNome) ? request.ItemId : request.ItemNome,
            Coins = request.Preco,
        });

        await _context.SaveChangesAsync();

        return Ok(new { coins = user.Coins - request.Preco });
    }

    // Lista SteamId + nível de todo jogador com VipNivel diferente de 0 —
    // útil pro mod sincronizar benefícios em lote em vez de consultar
    // jogador por jogador. Não filtra por expiração: reflete o campo bruto.
    [HttpPost("vips")]
    public async Task<IActionResult> GetVips(ListaVipsRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        var vips = await _context.Users
            .Where(u => u.Profile != null && u.Profile.SteamId != null && u.VipNivel != 0)
            .Select(u => new PlayerVipDto
            {
                SteamId = u.Profile!.SteamId!,
                VipNivel = u.VipNivel
            })
            .ToListAsync();

        return Ok(vips);
    }

    // Job do mod roda a cada ~15min e manda a posição de todos os veículos
    // segurados de todos os jogadores numa única chamada em lote (não uma
    // chamada por jogador). Um veículo só é atualizado se já estiver
    // vinculado a um seguro (CarroId preenchido numa sincronização anterior)
    // ou se existir um seguro ativo e ainda sem vínculo desse jogador — nesse
    // caso o vínculo é feito agora, na primeira sincronização desse carro.
    // Entradas sem usuário cadastrado ou sem seguro disponível pra vincular
    // são ignoradas silenciosamente, sem quebrar o resto do lote.
    [HttpPost("veiculos/posicao")]
    public async Task<IActionResult> SincronizarPosicoes(SincronizarPosicoesRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        var agora = DateTime.UtcNow;
        var atualizados = 0;

        // Evita que dois carrosId diferentes do mesmo jogador, sem vínculo
        // ainda, acabem "roubando" o mesmo seguro dentro do mesmo lote — a
        // query abaixo bate no banco e não vê vínculos feitos mais cedo
        // nesta mesma requisição (só ficam visíveis depois do SaveChanges).
        var segurosVinculadosNesteLote = new HashSet<Guid>();

        foreach (var veiculo in request.Veiculos)
        {
            if (string.IsNullOrWhiteSpace(veiculo.CarroId) || string.IsNullOrWhiteSpace(veiculo.SteamId))
                continue;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == veiculo.SteamId);

            if (user == null)
                continue;

            var seguro = await _context.Seguros
                .FirstOrDefaultAsync(s => s.CarroId == veiculo.CarroId && s.UserId == user.Id);

            if (seguro == null)
            {
                seguro = await _context.Seguros
                    .Where(s => s.UserId == user.Id
                        && s.CarroId == null
                        && s.ExpiraEm > agora
                        && !segurosVinculadosNesteLote.Contains(s.Id))
                    .OrderBy(s => s.CriadoEm)
                    .FirstOrDefaultAsync();

                if (seguro == null)
                    continue;

                seguro.CarroId = veiculo.CarroId;
            }

            segurosVinculadosNesteLote.Add(seguro.Id);

            seguro.VeiculoNome = veiculo.Nome;
            seguro.PosicaoGrid = veiculo.PosicaoGrid;
            seguro.PosicaoX = veiculo.X;
            seguro.PosicaoZ = veiculo.Z;
            seguro.PosicaoAtualizadaEm = agora;

            atualizados++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { atualizados });
    }

    // Intervalo mínimo entre dois resgates do mesmo seguro.
    private const int HorasCooldownResgate = 48;

    // Duração do seguro a partir da criação.
    private const int MesesDuracaoSeguro = 1;

    // Registra o seguro de um item comprado dentro do jogo (normalmente
    // veículo). Cada chamada cria um seguro novo — o mesmo jogador pode ter
    // vários do mesmo item, cada um com seu próprio cooldown. Dura 1 mês.
    [HttpPost("seguro")]
    public async Task<IActionResult> CriarSeguro(CriarSeguroRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Id))
            return BadRequest("Id do item é obrigatório.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var agora = DateTime.UtcNow;

        // Opcional — quando o mod já sabe qual veículo está sendo segurado
        // na hora da compra, vincula direto em vez de esperar a próxima
        // sincronização de posição. Só barra se outro seguro AINDA ATIVO já
        // usa esse CarroId — um seguro expirado antigo com o mesmo CarroId
        // não conta, senão o veículo nunca poderia ser resegurado.
        string? carroId = null;
        if (!string.IsNullOrWhiteSpace(request.CarroId))
        {
            carroId = request.CarroId.Trim();

            var carroJaSegurado = await _context.Seguros
                .AnyAsync(s => s.CarroId == carroId && s.ExpiraEm > agora);

            if (carroJaSegurado)
                return BadRequest("Esse veículo já tem um seguro ativo.");
        }

        var seguro = new Seguro
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = request.Id.Trim(),
            CriadoEm = agora,
            ExpiraEm = agora.AddMonths(MesesDuracaoSeguro),
            CarroId = carroId,
        };

        _context.Seguros.Add(seguro);

        await _context.SaveChangesAsync();

        return Ok(new { idSeguro = seguro.Id });
    }

    // Lista os seguros ativos (não expirados) do jogador e se cada um já
    // pode ser resgatado.
    [HttpPost("seguros")]
    public async Task<IActionResult> GetSeguros(ListaSegurosRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var seguros = await _context.Seguros
            .Where(s => s.UserId == user.Id && s.ExpiraEm > DateTime.UtcNow)
            .ToListAsync();

        var limite = DateTime.UtcNow.AddHours(-HorasCooldownResgate);

        var dtos = seguros
            .Select(s =>
            {
                var podeResgatar = s.UltimoResgate == null || s.UltimoResgate <= limite;

                return new SeguroDto
                {
                    IdSeguro = s.Id,
                    Id = s.ItemId,
                    PodeResgatar = podeResgatar,
                    ProximoResgateEm = podeResgatar
                        ? null
                        : s.UltimoResgate!.Value.AddHours(HorasCooldownResgate)
                };
            })
            .ToList();

        return Ok(dtos);
    }

    // Marca o seguro como resgatado agora, respeitando o cooldown — exceto no
    // resgate expresso (Pago = true), em que o mod já cobrou o jogador via
    // /api/game/comprar e o cooldown é pulado. Nos dois casos o timestamp é
    // atualizado, então o próximo resgate grátis conta a partir de agora.
    [HttpPost("seguro/resgate")]
    public async Task<IActionResult> ResgatarSeguro(ResgatarSeguroRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var seguro = await _context.Seguros
            .FirstOrDefaultAsync(s => s.Id == request.IdSeguro && s.UserId == user.Id);

        if (seguro == null)
            return NotFound();

        var agora = DateTime.UtcNow;

        if (seguro.ExpiraEm <= agora)
            return BadRequest("Esse seguro expirou.");

        // Opcional — o resgate recria o veículo no jogo com um CarroId novo,
        // então o mod manda o novo valor aqui pra manter o vínculo (senão o
        // seguro fica "sem carro" até a próxima sincronização de posição).
        // Mesma checagem de unicidade da criação, só que ignorando o próprio
        // seguro (ele já pode ser o dono do CarroId antigo).
        string? novoCarroId = null;
        if (!string.IsNullOrWhiteSpace(request.CarroId))
        {
            novoCarroId = request.CarroId.Trim();

            var carroJaSeguradoAltrove = await _context.Seguros
                .AnyAsync(s => s.Id != seguro.Id && s.CarroId == novoCarroId && s.ExpiraEm > agora);

            if (carroJaSeguradoAltrove)
                return BadRequest("Esse veículo já tem um seguro ativo.");
        }

        var limite = agora.AddHours(-HorasCooldownResgate);

        // UPDATE condicional em vez de ler-checar-salvar: se o mod disparar
        // dois resgates do mesmo seguro em paralelo, só um passa. No resgate
        // expresso a condição de cooldown sai, mas o UPDATE continua sendo a
        // única escrita — o comportamento concorrente não muda.
        var atualizacao = _context.Seguros.Where(s => s.Id == seguro.Id);

        if (!request.Pago)
            atualizacao = atualizacao.Where(s => s.UltimoResgate == null || s.UltimoResgate <= limite);

        // ExecuteUpdateAsync só aceita uma árvore de expressão (sem corpo em
        // bloco), então a ramificação de ter ou não um novo CarroId precisa
        // escolher entre duas chamadas inteiras, não montar uma condicional
        // dentro da lambda.
        var linhasAfetadas = novoCarroId != null
            ? await atualizacao.ExecuteUpdateAsync(sp => sp
                .SetProperty(s => s.UltimoResgate, agora)
                .SetProperty(s => s.CarroId, novoCarroId))
            : await atualizacao.ExecuteUpdateAsync(sp => sp
                .SetProperty(s => s.UltimoResgate, agora));

        if (linhasAfetadas == 0)
            return request.Pago
                ? NotFound()
                : BadRequest($"Esse seguro só pode ser resgatado novamente {HorasCooldownResgate}h depois do último resgate.");

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Tipo = "seguro",
            // O débito do resgate expresso já foi registrado pelo /comprar
            // (Tipo = "mod"), então aqui fica 0 pra não contar duas vezes.
            Descricao = request.Pago
                ? $"Resgate expresso de seguro: {seguro.ItemId}"
                : $"Resgate de seguro: {seguro.ItemId}",
            Coins = 0,
        });

        await _context.SaveChangesAsync();

        return Ok(new { proximoResgateEm = agora.AddHours(HorasCooldownResgate) });
    }

    // Sincroniza os totais absolutos de kills/deaths do jogador (o mod manda
    // o total atual, não um incremento) — mesma convenção de
    // SincronizarPosicoes pra veículos. Cria a linha de ranking na primeira
    // sincronização desse jogador.
    [HttpPost("ranking/kd")]
    public async Task<IActionResult> SincronizarKd(SincronizarKdRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        if (request.Kills < 0 || request.Deaths < 0)
            return BadRequest("Kills e Deaths não podem ser negativos.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var ranking = await _context.PlayerRankings
            .FirstOrDefaultAsync(r => r.UserId == user.Id);

        if (ranking == null)
        {
            ranking = new PlayerRanking { Id = Guid.NewGuid(), UserId = user.Id };
            _context.PlayerRankings.Add(ranking);
        }

        ranking.Kills = request.Kills;
        ranking.Deaths = request.Deaths;
        ranking.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok();
    }

    // Chamado uma vez a cada conclusão do KOTH — soma 1 ao contador, ao
    // contrário do K/D acima (que sincroniza um total absoluto). Também cria
    // a linha de ranking na primeira conclusão desse jogador.
    [HttpPost("ranking/koth")]
    public async Task<IActionResult> RegistrarKoth(RegistrarKothRequest request)
    {
        if (!ValidarApiKey(request.ApiKey))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.SteamId))
            return BadRequest("SteamId é obrigatório.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Profile != null && u.Profile.SteamId == request.SteamId);

        if (user == null)
            return NotFound();

        var ranking = await _context.PlayerRankings
            .FirstOrDefaultAsync(r => r.UserId == user.Id);

        if (ranking == null)
        {
            ranking = new PlayerRanking { Id = Guid.NewGuid(), UserId = user.Id };
            _context.PlayerRankings.Add(ranking);
        }

        ranking.KothCompletados++;
        ranking.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { kothCompletados = ranking.KothCompletados });
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
