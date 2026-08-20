using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class GrupoRankingTests : IDisposable
{
    private const string ApiKeyValida = "chave-secreta-do-mod";
    private const string SteamIdJogador = "76500000000000123";

    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private GameController CriarController()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GameServer:ApiKey"] = ApiKeyValida,
            })
            .Build();

        return new GameController(_db.Context, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
    }

    private User CriarUsuario(string steamId, string nome = "Jogador")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = steamId, Name = nome },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    [Fact]
    public async Task SincronizarKd_ComCamposNovos_GravaTodosOsValores()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarController();

        await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Kills = 12,
            Deaths = 5,
            ZumbiKills = 340,
            KothCompletados = 4,
            SegundosJogados = 45230,
        });

        var ranking = await _db.Context.PlayerRankings.SingleAsync(r => r.UserId == user.Id);

        Assert.Equal(12, ranking.Kills);
        Assert.Equal(5, ranking.Deaths);
        Assert.Equal(340, ranking.ZumbiKills);
        Assert.Equal(4, ranking.KothCompletados);
        Assert.Equal(45230, ranking.SegundosJogados);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, -1, 0, 0)]
    [InlineData(0, 0, 0, -1, 0)]
    [InlineData(0, 0, 0, 0, -1)]
    public async Task SincronizarKd_ComValorNegativo_RetornaBadRequest(
        int kills, int deaths, int zumbiKills, int kothCompletados, int segundosJogados)
    {
        CriarUsuario(SteamIdJogador);
        var controller = CriarController();

        var resultado = await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Kills = kills,
            Deaths = deaths,
            ZumbiKills = zumbiKills,
            KothCompletados = kothCompletados,
            SegundosJogados = segundosJogados,
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task GetRankingJogador_ApiKeyInvalida_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.GetRankingJogador(new PlayerLookupRequest { ApiKey = "chave-errada", SteamId = SteamIdJogador });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task GetRankingJogador_JogadorComRanking_RetornaResumo()
    {
        var user = CriarUsuario(SteamIdJogador, nome: "Fulano");
        _db.Context.PlayerRankings.Add(new PlayerRanking
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Kills = 12,
            Deaths = 5,
            ZumbiKills = 340,
            KothCompletados = 4,
            SegundosJogados = 45230,
        });
        _db.Context.SaveChanges();

        var controller = CriarController();

        var resultado = await controller.GetRankingJogador(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<JogadorRankingDto>(ok.Value);

        Assert.Equal(SteamIdJogador, dto.SteamId);
        Assert.Equal("Fulano", dto.Nome);
        Assert.Equal(12, dto.Kills);
        Assert.Equal(340, dto.ZumbiKills);
        Assert.Equal(45230, dto.SegundosJogados);
    }

    [Fact]
    public async Task GetRankingJogador_SteamIdDesconhecido_RetornaNotFound()
    {
        var controller = CriarController();

        var resultado = await controller.GetRankingJogador(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = "76500000000000999" });

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task AdicionarAoGrupo_GrupoInexistente_RetornaNotFound()
    {
        var controller = CriarController();

        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = "grupo-que-nao-existe",
            SteamId = SteamIdJogador,
        });

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task AdicionarAoGrupo_PorGrupoModId_AdicionaComoMembroComum()
    {
        var lider = CriarUsuario("76500000000000999");
        var cla = new Cla { Id = Guid.NewGuid(), GrupoModId = "grupo-1", Nome = "Grupo", LiderUserId = lider.Id, LiderSteamId = "76500000000000999" };
        _db.Context.Clas.Add(cla);
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, SteamId = "76500000000000999", IsAdmin = true, EntrouEm = DateTime.UtcNow });
        _db.Context.SaveChanges();

        var controller = CriarController();

        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = "grupo-1",
            SteamId = SteamIdJogador,
        });

        Assert.IsType<OkResult>(resultado);

        var membros = await _db.Context.ClaMembros.Where(m => m.ClaId == cla.Id).ToListAsync();
        Assert.Equal(2, membros.Count);
        Assert.False(membros.Single(m => m.SteamId == SteamIdJogador).IsAdmin);
    }

    [Fact]
    public async Task AdicionarAoGrupo_PorGuidInternoDeClaSemGrupoModId_Adiciona()
    {
        var cla = CriarClaDireto(SteamIdJogador, nome: "Clã do Site");
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = cla.Id.ToString(),
            SteamId = "76500000000000456",
        });

        Assert.IsType<OkResult>(resultado);
        Assert.Equal(2, await _db.Context.ClaMembros.CountAsync(m => m.ClaId == cla.Id));
    }

    [Fact]
    public async Task AdicionarAoGrupo_JaNoLimiteDeMembros_RetornaBadRequest()
    {
        var cla = CriarClaDireto(SteamIdJogador);
        for (var i = 0; i < ClaLimites.MaxMembros; i++)
            AdicionarMembroDireto(cla, $"7650000000000{1000 + i}", isAdmin: i == 0, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = cla.Id.ToString(),
            SteamId = "76500000000009999",
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Equal(ClaLimites.MaxMembros, await _db.Context.ClaMembros.CountAsync(m => m.ClaId == cla.Id));
    }

    [Fact]
    public async Task AdicionarAoGrupo_JaEhMembroDoMesmoCla_Idempotente()
    {
        var cla = CriarClaDireto(SteamIdJogador);
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = cla.Id.ToString(),
            SteamId = SteamIdJogador,
        });

        Assert.IsType<OkResult>(resultado);
        Assert.Single(await _db.Context.ClaMembros.Where(m => m.ClaId == cla.Id).ToListAsync());
    }

    [Fact]
    public async Task AdicionarAoGrupo_JogadorJaEmOutroGrupo_MoveVinculo()
    {
        var claA = CriarClaDireto(SteamIdJogador, nome: "A");
        AdicionarMembroDireto(claA, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow);

        var claB = CriarClaDireto("76500000000000456", nome: "B");
        AdicionarMembroDireto(claB, "76500000000000456", isAdmin: true, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        // Jogador saiu do grupo A no jogo e entrou no B, sem chamar expulsar antes.
        var resultado = await controller.AdicionarAoGrupo(new GrupoAdicionarRequest
        {
            ApiKey = ApiKeyValida,
            Id = claB.Id.ToString(),
            SteamId = SteamIdJogador,
        });

        Assert.IsType<OkResult>(resultado);

        var vinculo = await _db.Context.ClaMembros.SingleAsync(m => m.SteamId == SteamIdJogador);
        Assert.Equal(claB.Id, vinculo.ClaId);

        // Grupo A continua existindo (só ficou sem esse membro) — dissolver
        // por esvaziar é responsabilidade do /grupos/expulsar, não daqui.
        Assert.True(await _db.Context.Clas.AnyAsync(c => c.Id == claA.Id));
    }

    [Fact]
    public async Task GetGrupoJogador_MembroDeUmGrupo_RetornaGrupo()
    {
        var cla = CriarClaDireto(SteamIdJogador, nome: "Grupo de Fulano");
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow);
        AdicionarMembroDireto(cla, "76500000000000456", isAdmin: false, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        var resultado = await controller.GetGrupoJogador(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<GrupoJogadorDto>(ok.Value);

        Assert.True(dto.TemGrupo);
        Assert.Equal("Grupo de Fulano", dto.Nome);
    }

    [Fact]
    public async Task GetGrupoJogador_SemGrupo_RetornaTemGrupoFalso()
    {
        var controller = CriarController();

        var resultado = await controller.GetGrupoJogador(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<GrupoJogadorDto>(ok.Value);

        Assert.False(dto.TemGrupo);
    }

    private Cla CriarClaDireto(string liderSteamId, string nome = "Clã de Teste")
    {
        var cla = new Cla { Id = Guid.NewGuid(), Nome = nome, LiderSteamId = liderSteamId, CriadoEm = DateTime.UtcNow };
        _db.Context.Clas.Add(cla);
        _db.Context.SaveChanges();

        return cla;
    }

    private void AdicionarMembroDireto(Cla cla, string steamId, bool isAdmin, DateTime entrouEm)
    {
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, SteamId = steamId, IsAdmin = isAdmin, EntrouEm = entrouEm });
        _db.Context.SaveChanges();
    }

    [Fact]
    public async Task ExpulsarDoGrupo_ApiKeyInvalida_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = "chave-errada", SteamId = SteamIdJogador });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task ExpulsarDoGrupo_JogadorSemGrupo_RetornaNotFound()
    {
        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task ExpulsarDoGrupo_MembroComum_SoRemoveMantemLider()
    {
        var cla = CriarClaDireto(SteamIdJogador);
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow.AddDays(-2));
        AdicionarMembroDireto(cla, "76500000000000456", isAdmin: false, entrouEm: DateTime.UtcNow.AddDays(-1));

        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = "76500000000000456" });

        Assert.IsType<OkObjectResult>(resultado);
        Assert.False(await _db.Context.ClaMembros.AnyAsync(m => m.SteamId == "76500000000000456"));

        var claAtualizado = await _db.Context.Clas.SingleAsync(c => c.Id == cla.Id);
        Assert.Equal(SteamIdJogador, claAtualizado.LiderSteamId);
    }

    [Fact]
    public async Task ExpulsarDoGrupo_LiderComAdminDisponivel_PromoveAdminMaisAntigo()
    {
        const string admin1 = "76500000000000456";
        const string admin2 = "76500000000000789";
        const string membroComum = "76500000000000111";

        var cla = CriarClaDireto(SteamIdJogador);
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow.AddDays(-5));
        // Membro comum entrou antes dos admins, mas admin tem prioridade mesmo assim.
        AdicionarMembroDireto(cla, membroComum, isAdmin: false, entrouEm: DateTime.UtcNow.AddDays(-4));
        AdicionarMembroDireto(cla, admin2, isAdmin: true, entrouEm: DateTime.UtcNow.AddDays(-2));
        AdicionarMembroDireto(cla, admin1, isAdmin: true, entrouEm: DateTime.UtcNow.AddDays(-3));

        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var valor = ok.Value!;
        var novoLider = valor.GetType().GetProperty("novoLiderSteamId")!.GetValue(valor);
        Assert.Equal(admin1, novoLider);

        var claAtualizado = await _db.Context.Clas.SingleAsync(c => c.Id == cla.Id);
        Assert.Equal(admin1, claAtualizado.LiderSteamId);

        var novoLiderMembro = await _db.Context.ClaMembros.SingleAsync(m => m.SteamId == admin1);
        Assert.True(novoLiderMembro.IsAdmin);
    }

    [Fact]
    public async Task ExpulsarDoGrupo_LiderSemAdminComMembroComum_PromoveMembroMaisAntigo()
    {
        const string membro1 = "76500000000000456";
        const string membro2 = "76500000000000789";

        var cla = CriarClaDireto(SteamIdJogador);
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow.AddDays(-5));
        AdicionarMembroDireto(cla, membro2, isAdmin: false, entrouEm: DateTime.UtcNow.AddDays(-2));
        AdicionarMembroDireto(cla, membro1, isAdmin: false, entrouEm: DateTime.UtcNow.AddDays(-3));

        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        Assert.IsType<OkObjectResult>(resultado);

        var claAtualizado = await _db.Context.Clas.SingleAsync(c => c.Id == cla.Id);
        Assert.Equal(membro1, claAtualizado.LiderSteamId);

        var novoLiderMembro = await _db.Context.ClaMembros.SingleAsync(m => m.SteamId == membro1);
        Assert.True(novoLiderMembro.IsAdmin);
    }

    [Fact]
    public async Task ExpulsarDoGrupo_LiderUltimoMembro_ApagaCla()
    {
        var cla = CriarClaDireto(SteamIdJogador);
        AdicionarMembroDireto(cla, SteamIdJogador, isAdmin: true, entrouEm: DateTime.UtcNow);

        var controller = CriarController();

        var resultado = await controller.ExpulsarDoGrupo(new PlayerLookupRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var valor = ok.Value!;
        var claApagado = (bool)valor.GetType().GetProperty("claApagado")!.GetValue(valor)!;
        Assert.True(claApagado);

        Assert.False(await _db.Context.Clas.AnyAsync(c => c.Id == cla.Id));
        Assert.False(await _db.Context.ClaMembros.AnyAsync(m => m.ClaId == cla.Id));
    }
}
