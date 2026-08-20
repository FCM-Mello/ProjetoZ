using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

// Clã e "Grupo" (o que o mod sincroniza) são a mesma entidade (Cla) — esse
// controller é o lado site (JWT): criar, entrar, promover, sair, desfazer.
// O lado mod fica no GameController (sync em lote + leitura por SteamId).
// Membro é identificado por SteamId (não UserId) porque um clã de origem
// mod pode ter jogador que nunca logou no site.
[ApiController]
[Route("api/clas")]
[Authorize]
public class ClasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClasController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid? MeuId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : Guid.Parse(userId);
    }

    private async Task<string?> SteamIdDoUsuario(Guid userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Profile != null ? u.Profile.SteamId : null)
            .FirstOrDefaultAsync();
    }

    [HttpGet]
    public async Task<IActionResult> GetTodos()
    {
        var brutos = await (
            from c in _context.Clas
            join lider in _context.Users on c.LiderUserId equals lider.Id into gj
            from lider in gj.DefaultIfEmpty()
            select new
            {
                c.Id,
                c.Nome,
                c.Descricao,
                c.Estandarte,
                LiderNome = lider != null && lider.Profile != null ? lider.Profile.Name : null,
            }
        ).ToListAsync();

        var contagem = await _context.ClaMembros
            .GroupBy(m => m.ClaId)
            .Select(g => new { ClaId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ClaId, x => x.Total);

        var dto = brutos
            .Select(c => new ClaResumoDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Descricao = c.Descricao,
                Estandarte = c.Estandarte,
                LiderNome = c.LiderNome ?? "Jogador",
                TotalMembros = contagem.TryGetValue(c.Id, out var total) ? total : 0,
            })
            .OrderByDescending(c => c.TotalMembros)
            .ToList();

        return Ok(dto);
    }

    [HttpGet("meu")]
    public async Task<IActionResult> GetMeu()
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value);
        if (minhaSteamId == null)
            return NoContent();

        var membro = await _context.ClaMembros.FirstOrDefaultAsync(m => m.SteamId == minhaSteamId);
        if (membro == null)
            return NoContent();

        return await MontarDetalhe(membro.ClaId, meuId.Value, minhaSteamId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPorId(Guid id)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        return await MontarDetalhe(id, meuId.Value, minhaSteamId);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CriarClaRequest request)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value);
        if (string.IsNullOrEmpty(minhaSteamId))
            return BadRequest("Sua conta precisa estar vinculada à Steam.");

        var jaTemCla = await _context.ClaMembros.AnyAsync(m => m.SteamId == minhaSteamId);
        if (jaTemCla)
            return BadRequest("Você já faz parte de um clã.");

        var nomeEmUso = await _context.Clas.AnyAsync(c => c.Nome == request.Nome);
        if (nomeEmUso)
            return BadRequest("Já existe um clã com esse nome.");

        var cla = new Cla
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Descricao = request.Descricao,
            Estandarte = request.Estandarte,
            LiderUserId = meuId.Value,
            LiderSteamId = minhaSteamId,
        };
        _context.Clas.Add(cla);

        _context.ClaMembros.Add(new ClaMembro
        {
            Id = Guid.NewGuid(),
            ClaId = cla.Id,
            UserId = meuId.Value,
            SteamId = minhaSteamId,
            IsAdmin = true,
        });

        await _context.SaveChangesAsync();

        return Ok(new { id = cla.Id });
    }

    [HttpPost("{id}/solicitar")]
    public async Task<IActionResult> Solicitar(Guid id)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value);
        if (string.IsNullOrEmpty(minhaSteamId))
            return BadRequest("Sua conta precisa estar vinculada à Steam.");

        var claExiste = await _context.Clas.AnyAsync(c => c.Id == id);
        if (!claExiste)
            return NotFound();

        var jaTemCla = await _context.ClaMembros.AnyAsync(m => m.SteamId == minhaSteamId);
        if (jaTemCla)
            return BadRequest("Você já faz parte de um clã.");

        var jaSolicitou = await _context.ClaSolicitacoes.AnyAsync(s => s.ClaId == id && s.UserId == meuId);
        if (jaSolicitou)
            return BadRequest("Você já solicitou entrada nesse clã.");

        var totalMembros = await _context.ClaMembros.CountAsync(m => m.ClaId == id);
        if (totalMembros >= ClaLimites.MaxMembros)
            return BadRequest($"Esse clã já está no limite de {ClaLimites.MaxMembros} membros.");

        _context.ClaSolicitacoes.Add(new ClaSolicitacao
        {
            Id = Guid.NewGuid(),
            ClaId = id,
            UserId = meuId.Value,
        });

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/solicitacoes/{solicitacaoId}/aprovar")]
    public async Task<IActionResult> AprovarSolicitacao(Guid id, Guid solicitacaoId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (!await SouAdminOuLider(cla, minhaSteamId))
            return Forbid();

        var solicitacao = await _context.ClaSolicitacoes
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId && s.ClaId == id);
        if (solicitacao == null)
            return NotFound();

        var solicitanteSteamId = await SteamIdDoUsuario(solicitacao.UserId);
        if (string.IsNullOrEmpty(solicitanteSteamId))
        {
            _context.ClaSolicitacoes.Remove(solicitacao);
            await _context.SaveChangesAsync();

            return BadRequest("Esse jogador não tem mais uma conta válida.");
        }

        // O solicitante pode ter entrado em outro clã enquanto o pedido
        // esperava aprovação — nesse caso só descarta o pedido.
        var jaTemCla = await _context.ClaMembros.AnyAsync(m => m.SteamId == solicitanteSteamId);
        if (jaTemCla)
        {
            _context.ClaSolicitacoes.Remove(solicitacao);
            await _context.SaveChangesAsync();

            return BadRequest("Esse jogador já entrou em outro clã.");
        }

        var totalMembros = await _context.ClaMembros.CountAsync(m => m.ClaId == id);
        if (totalMembros >= ClaLimites.MaxMembros)
            return BadRequest($"Esse clã já está no limite de {ClaLimites.MaxMembros} membros.");

        _context.ClaMembros.Add(new ClaMembro
        {
            Id = Guid.NewGuid(),
            ClaId = id,
            UserId = solicitacao.UserId,
            SteamId = solicitanteSteamId,
            IsAdmin = false,
        });

        // Limpa outras solicitações pendentes do mesmo jogador — só pode
        // estar em um clã, não faz sentido elas continuarem existindo.
        var outrasSolicitacoes = _context.ClaSolicitacoes.Where(s => s.UserId == solicitacao.UserId);
        _context.ClaSolicitacoes.RemoveRange(outrasSolicitacoes);

        await _context.SaveChangesAsync();

        return Ok();
    }

    // Rejeitar (líder/admin) ou cancelar o próprio pedido (o solicitante).
    [HttpDelete("{id}/solicitacoes/{solicitacaoId}")]
    public async Task<IActionResult> RemoverSolicitacao(Guid id, Guid solicitacaoId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var solicitacao = await _context.ClaSolicitacoes
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId && s.ClaId == id);
        if (solicitacao == null)
            return NotFound();

        if (solicitacao.UserId != meuId)
        {
            var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

            var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
            if (cla == null)
                return NotFound();

            if (!await SouAdminOuLider(cla, minhaSteamId))
                return Forbid();
        }

        _context.ClaSolicitacoes.Remove(solicitacao);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/membros/{userId}/promover")]
    public async Task<IActionResult> PromoverAdmin(Guid id, Guid userId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (!await SouAdminOuLider(cla, minhaSteamId))
            return Forbid();

        if (userId == cla.LiderUserId)
            return BadRequest("O líder já tem controle total do clã.");

        var membro = await _context.ClaMembros.FirstOrDefaultAsync(m => m.ClaId == id && m.UserId == userId);
        if (membro == null)
            return NotFound();

        if (membro.IsAdmin)
            return BadRequest("Esse membro já é admin.");

        membro.IsAdmin = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Só o líder pode tirar admin de outro membro — admins não podem
    // rebaixar outros admins entre si.
    [HttpPost("{id}/membros/{userId}/remover-admin")]
    public async Task<IActionResult> RemoverAdmin(Guid id, Guid userId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (cla.LiderUserId != meuId)
            return Forbid();

        if (userId == cla.LiderUserId)
            return BadRequest("O líder não pode perder o próprio cargo — precisa desfazer o clã.");

        var membro = await _context.ClaMembros.FirstOrDefaultAsync(m => m.ClaId == id && m.UserId == userId);
        if (membro == null)
            return NotFound();

        if (!membro.IsAdmin)
            return BadRequest("Esse membro não é admin.");

        membro.IsAdmin = false;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Só o líder pode expulsar um membro — inclui admins (pra tirar um
    // admin do clã de vez, não só o cargo, o líder usa esse em vez do
    // RemoverAdmin). O próprio líder não pode se auto-expulsar, precisa
    // desfazer o clã.
    [HttpDelete("{id}/membros/{userId}")]
    public async Task<IActionResult> RemoverMembro(Guid id, Guid userId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (cla.LiderUserId != meuId)
            return Forbid();

        if (userId == cla.LiderUserId)
            return BadRequest("O líder não pode se auto-expulsar — precisa desfazer o clã.");

        var membro = await _context.ClaMembros.FirstOrDefaultAsync(m => m.ClaId == id && m.UserId == userId);
        if (membro == null)
            return NotFound();

        _context.ClaMembros.Remove(membro);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/sair")]
    public async Task<IActionResult> Sair(Guid id)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (cla.LiderSteamId == minhaSteamId)
            return BadRequest("O líder não pode sair — só desfazer o clã.");

        var membro = await _context.ClaMembros.FirstOrDefaultAsync(m => m.ClaId == id && m.SteamId == minhaSteamId);
        if (membro == null)
            return NotFound();

        _context.ClaMembros.Remove(membro);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desfazer(Guid id)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (cla.LiderUserId != meuId)
            return Forbid();

        await _context.ClaMembros.Where(m => m.ClaId == id).ExecuteDeleteAsync();
        await _context.ClaSolicitacoes.Where(s => s.ClaId == id).ExecuteDeleteAsync();

        var convitesDoCla = await _context.ClaConvites.Where(c => c.ClaId == id).ToListAsync();
        foreach (var convite in convitesDoCla)
            await RemoverNotificacaoDoConvite(convite.Id);

        _context.ClaConvites.RemoveRange(convitesDoCla);
        _context.Clas.Remove(cla);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Líder/admin busca um jogador (por nome ou SteamId) pra convidar —
    // qualquer usuário cadastrado serve, mesmo que já esteja em outro clã
    // (aceitar o convite tira ele de lá).
    [HttpGet("{id}/buscar-jogador")]
    public async Task<IActionResult> BuscarJogador(Guid id, [FromQuery] string q)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (!await SouAdminOuLider(cla, minhaSteamId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new List<ClaBuscaJogadorDto>());

        var termo = q.Trim().ToLower();

        var resultados = await _context.Users
            .Where(u => u.Profile != null
                && ((u.Profile.Name != null && u.Profile.Name.ToLower().Contains(termo))
                    || (u.Profile.SteamId != null && u.Profile.SteamId.Contains(termo))))
            .OrderBy(u => u.Profile!.Name)
            .Take(10)
            .Select(u => new ClaBuscaJogadorDto
            {
                UserId = u.Id,
                Nome = u.Profile!.Name ?? "Jogador",
                Avatar = u.Profile.Avatar ?? string.Empty,
            })
            .ToListAsync();

        return Ok(resultados);
    }

    [HttpPost("{id}/convidar/{userId}")]
    public async Task<IActionResult> Convidar(Guid id, Guid userId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value) ?? string.Empty;

        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == id);
        if (cla == null)
            return NotFound();

        if (!await SouAdminOuLider(cla, minhaSteamId))
            return Forbid();

        var convidadoExiste = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!convidadoExiste)
            return NotFound();

        var jaEhMembro = await _context.ClaMembros.AnyAsync(m => m.ClaId == id && m.UserId == userId);
        if (jaEhMembro)
            return BadRequest("Esse jogador já é membro do clã.");

        var jaConvidado = await _context.ClaConvites.AnyAsync(c => c.ClaId == id && c.ConvidadoUserId == userId);
        if (jaConvidado)
            return BadRequest("Esse jogador já tem um convite pendente pra esse clã.");

        var totalMembros = await _context.ClaMembros.CountAsync(m => m.ClaId == id);
        if (totalMembros >= ClaLimites.MaxMembros)
            return BadRequest($"Esse clã já está no limite de {ClaLimites.MaxMembros} membros.");

        var convite = new ClaConvite
        {
            Id = Guid.NewGuid(),
            ClaId = id,
            ConvidadoUserId = userId,
            ConvidadoPorUserId = meuId.Value,
        };
        _context.ClaConvites.Add(convite);

        var agora = DateTime.UtcNow;
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            Titulo = "Convite de clã",
            Mensagem = $"Você foi convidado para o clã \"{cla.Nome}\".",
            Nivel = "amarelo",
            CriadoEm = agora,
            CriadoPorUserId = meuId.Value,
            EnviarEm = agora,
            ExpiraEm = agora.AddDays(NotificacaoNiveis.DiasAteExpirar),
            ParaTodos = false,
            Tipo = "convite_cla",
            ClaConviteId = convite.Id,
        };
        _context.Notificacoes.Add(notificacao);

        _context.NotificacaoDestinatarios.Add(new NotificacaoDestinatario
        {
            Id = Guid.NewGuid(),
            NotificacaoId = notificacao.Id,
            UserId = userId,
        });

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("convites/{conviteId}/aceitar")]
    public async Task<IActionResult> AceitarConvite(Guid conviteId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var convite = await _context.ClaConvites.FirstOrDefaultAsync(c => c.Id == conviteId);
        if (convite == null)
            return NotFound();

        if (convite.ConvidadoUserId != meuId)
            return Forbid();

        var minhaSteamId = await SteamIdDoUsuario(meuId.Value);
        if (string.IsNullOrEmpty(minhaSteamId))
            return BadRequest("Sua conta precisa estar vinculada à Steam.");

        var totalMembros = await _context.ClaMembros.CountAsync(m => m.ClaId == convite.ClaId);
        if (totalMembros >= ClaLimites.MaxMembros)
            return BadRequest($"Esse clã já está no limite de {ClaLimites.MaxMembros} membros.");

        // Sai do clã antigo, se tiver — a verdade do convite aceito vence.
        var membroAntigo = await _context.ClaMembros.FirstOrDefaultAsync(m => m.SteamId == minhaSteamId);
        if (membroAntigo != null)
            _context.ClaMembros.Remove(membroAntigo);

        _context.ClaMembros.Add(new ClaMembro
        {
            Id = Guid.NewGuid(),
            ClaId = convite.ClaId,
            UserId = meuId.Value,
            SteamId = minhaSteamId,
            IsAdmin = false,
        });

        // Outros convites pendentes pro mesmo jogador (de outros clãs) ficam
        // sem sentido — só pode estar em um clã por vez.
        var outrosConvites = await _context.ClaConvites
            .Where(c => c.ConvidadoUserId == meuId && c.Id != conviteId)
            .ToListAsync();

        foreach (var outro in outrosConvites)
            await RemoverNotificacaoDoConvite(outro.Id);

        _context.ClaConvites.RemoveRange(outrosConvites);

        await RemoverNotificacaoDoConvite(convite.Id);
        _context.ClaConvites.Remove(convite);

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("convites/{conviteId}/recusar")]
    public async Task<IActionResult> RecusarConvite(Guid conviteId)
    {
        var meuId = MeuId();
        if (meuId == null)
            return Unauthorized();

        var convite = await _context.ClaConvites.FirstOrDefaultAsync(c => c.Id == conviteId);
        if (convite == null)
            return NotFound();

        if (convite.ConvidadoUserId != meuId)
            return Forbid();

        await RemoverNotificacaoDoConvite(convite.Id);
        _context.ClaConvites.Remove(convite);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task RemoverNotificacaoDoConvite(Guid conviteId)
    {
        var notificacao = await _context.Notificacoes.FirstOrDefaultAsync(n => n.ClaConviteId == conviteId);
        if (notificacao == null)
            return;

        _context.NotificacaoDestinatarios.RemoveRange(
            _context.NotificacaoDestinatarios.Where(d => d.NotificacaoId == notificacao.Id));

        _context.NotificacaoLeituras.RemoveRange(
            _context.NotificacaoLeituras.Where(l => l.NotificacaoId == notificacao.Id));

        _context.Notificacoes.Remove(notificacao);
    }

    private async Task<bool> SouAdminOuLider(Cla cla, string minhaSteamId)
    {
        if (!string.IsNullOrEmpty(minhaSteamId) && cla.LiderSteamId == minhaSteamId)
            return true;

        return await _context.ClaMembros.AnyAsync(m => m.ClaId == cla.Id && m.SteamId == minhaSteamId && m.IsAdmin);
    }

    private async Task<IActionResult> MontarDetalhe(Guid claId, Guid meuId, string minhaSteamId)
    {
        var cla = await _context.Clas.FirstOrDefaultAsync(c => c.Id == claId);
        if (cla == null)
            return NotFound();

        var membrosBrutos = await (
            from m in _context.ClaMembros
            join u in _context.Users on m.UserId equals u.Id into gjUsers
            from u in gjUsers.DefaultIfEmpty()
            join r in _context.PlayerRankings on m.UserId equals r.UserId into gjRanking
            from r in gjRanking.DefaultIfEmpty()
            where m.ClaId == claId
            select new
            {
                m.UserId,
                m.SteamId,
                m.IsAdmin,
                Nome = u != null && u.Profile != null ? u.Profile.Name : null,
                Avatar = u != null && u.Profile != null ? u.Profile.Avatar : null,
                Kills = r != null ? r.Kills : 0,
                Deaths = r != null ? r.Deaths : 0,
                KothCompletados = r != null ? r.KothCompletados : 0,
                ZumbiKills = r != null ? r.ZumbiKills : 0,
                SegundosJogados = r != null ? r.SegundosJogados : 0,
            }
        ).ToListAsync();

        var meuMembro = membrosBrutos.FirstOrDefault(m => m.SteamId == minhaSteamId);
        var souLider = cla.LiderSteamId == minhaSteamId;
        var souAdmin = souLider || (meuMembro?.IsAdmin ?? false);

        var dto = new ClaDetalheDto
        {
            Id = cla.Id,
            Nome = cla.Nome,
            Descricao = cla.Descricao,
            Estandarte = cla.Estandarte,
            CriadoEm = cla.CriadoEm,
            SouLider = souLider,
            SouAdmin = souAdmin,
            Membros = membrosBrutos
                .Select(m => new ClaMembroDto
                {
                    UserId = m.UserId,
                    Nome = m.Nome ?? "Jogador",
                    Avatar = m.Avatar ?? string.Empty,
                    IsLider = m.SteamId == cla.LiderSteamId,
                    IsAdmin = m.IsAdmin,
                    Kills = m.Kills,
                    Deaths = m.Deaths,
                    Kd = RankingCalculos.CalcularKd(m.Kills, m.Deaths),
                    KothCompletados = m.KothCompletados,
                    ZumbiKills = m.ZumbiKills,
                    SegundosJogados = m.SegundosJogados,
                })
                .OrderByDescending(m => m.IsLider)
                .ThenByDescending(m => m.IsAdmin)
                .ThenBy(m => m.Nome)
                .ToList(),
            Estatisticas = new ClaEstatisticasDto
            {
                TotalKills = membrosBrutos.Sum(m => m.Kills),
                TotalDeaths = membrosBrutos.Sum(m => m.Deaths),
                KdMedio = RankingCalculos.CalcularKd(membrosBrutos.Sum(m => m.Kills), membrosBrutos.Sum(m => m.Deaths)),
                TotalKothCompletados = membrosBrutos.Sum(m => m.KothCompletados),
                TotalZumbiKills = membrosBrutos.Sum(m => m.ZumbiKills),
                TotalSegundosJogados = membrosBrutos.Sum(m => m.SegundosJogados),
            },
        };

        if (souAdmin)
        {
            dto.Solicitacoes = await (
                from s in _context.ClaSolicitacoes
                join u in _context.Users on s.UserId equals u.Id
                where s.ClaId == claId
                orderby s.CriadoEm
                select new ClaSolicitacaoDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Nome = u.Profile != null && u.Profile.Name != null ? u.Profile.Name : "Jogador",
                    Avatar = u.Profile != null && u.Profile.Avatar != null ? u.Profile.Avatar : string.Empty,
                    CriadoEm = s.CriadoEm,
                }
            ).ToListAsync();
        }
        else
        {
            dto.TenhoSolicitacaoPendente = await _context.ClaSolicitacoes
                .AnyAsync(s => s.ClaId == claId && s.UserId == meuId);
        }

        return Ok(dto);
    }
}
