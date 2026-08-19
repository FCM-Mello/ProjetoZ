using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
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

    private GameController CriarController(string? apiKeyNoHeader = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GameServer:ApiKey"] = ApiKeyValida,
            })
            .Build();

        var controller = new GameController(_db.Context, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        if (apiKeyNoHeader != null)
            controller.ControllerContext.HttpContext.Request.Headers["X-Api-Key"] = apiKeyNoHeader;

        return controller;
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
    public async Task GetRankingJogador_SemChaveNoHeader_RetornaUnauthorized()
    {
        var controller = CriarController(apiKeyNoHeader: "chave-errada");

        var resultado = await controller.GetRankingJogador(SteamIdJogador);

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

        var controller = CriarController(apiKeyNoHeader: ApiKeyValida);

        var resultado = await controller.GetRankingJogador(SteamIdJogador);

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
        var controller = CriarController(apiKeyNoHeader: ApiKeyValida);

        var resultado = await controller.GetRankingJogador("76500000000000999");

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task SincronizarGrupos_ComGrupoValido_Cria()
    {
        var controller = CriarController();

        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos =
            [
                new GrupoSyncItemDto
                {
                    Id = "1755500000-482913",
                    Nome = "Grupo de Fulano",
                    LiderSteamId = SteamIdJogador,
                    Membros = [SteamIdJogador, "76500000000000456"],
                },
            ],
        });

        var cla = await _db.Context.Clas.SingleAsync();

        Assert.Equal("Grupo de Fulano", cla.Nome);
        Assert.Equal(SteamIdJogador, cla.LiderSteamId);
        Assert.Equal("1755500000-482913", cla.GrupoModId);

        var membros = await _db.Context.ClaMembros.Where(m => m.ClaId == cla.Id).ToListAsync();
        Assert.Equal(2, membros.Count);
    }

    [Fact]
    public async Task SincronizarGrupos_GrupoQueSumiuDoLote_EhRemovido()
    {
        var controller = CriarController();

        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos = [new GrupoSyncItemDto { Id = "grupo-antigo", Nome = "Antigo", LiderSteamId = SteamIdJogador, Membros = [SteamIdJogador] }],
        });

        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos = [new GrupoSyncItemDto { Id = "grupo-novo", Nome = "Novo", LiderSteamId = SteamIdJogador, Membros = [SteamIdJogador] }],
        });

        var cla = await _db.Context.Clas.SingleAsync();
        Assert.Equal("grupo-novo", cla.GrupoModId);
    }

    [Fact]
    public async Task SincronizarGrupos_NaoApagaClaCriadoNoSite()
    {
        var lider = CriarUsuario("76500000000000999");
        _db.Context.Clas.Add(new Cla { Id = Guid.NewGuid(), Nome = "Clã do Site", LiderUserId = lider.Id, LiderSteamId = "76500000000000999" });
        _db.Context.SaveChanges();

        var controller = CriarController();

        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos = [new GrupoSyncItemDto { Id = "grupo-do-mod", Nome = "Do Mod", LiderSteamId = SteamIdJogador, Membros = [SteamIdJogador] }],
        });

        Assert.Equal(2, await _db.Context.Clas.CountAsync());
        Assert.True(await _db.Context.Clas.AnyAsync(c => c.Nome == "Clã do Site" && c.GrupoModId == null));
    }

    [Fact]
    public async Task SincronizarGrupos_JogadorMudouDeGrupo_MoveVinculo()
    {
        var controller = CriarController();

        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos =
            [
                new GrupoSyncItemDto { Id = "grupo-a", Nome = "A", LiderSteamId = SteamIdJogador, Membros = [SteamIdJogador] },
                new GrupoSyncItemDto { Id = "grupo-b", Nome = "B", LiderSteamId = "76500000000000456", Membros = ["76500000000000456"] },
            ],
        });

        // O jogador saiu do grupo A e entrou no B, sem passar pelo grupo A de novo nesse lote.
        await controller.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos =
            [
                new GrupoSyncItemDto { Id = "grupo-a", Nome = "A", LiderSteamId = SteamIdJogador, Membros = [] },
                new GrupoSyncItemDto { Id = "grupo-b", Nome = "B", LiderSteamId = "76500000000000456", Membros = ["76500000000000456", SteamIdJogador] },
            ],
        });

        var vinculo = await _db.Context.ClaMembros.SingleAsync(m => m.SteamId == SteamIdJogador);
        var claDoVinculo = await _db.Context.Clas.SingleAsync(c => c.Id == vinculo.ClaId);
        Assert.Equal("grupo-b", claDoVinculo.GrupoModId);
    }

    [Fact]
    public async Task GetGrupoJogador_MembroDeUmGrupo_RetornaGrupo()
    {
        var controller0 = CriarController();
        await controller0.SincronizarGrupos(new GrupoSyncRequest
        {
            ApiKey = ApiKeyValida,
            Grupos = [new GrupoSyncItemDto { Id = "1755500000-482913", Nome = "Grupo de Fulano", LiderSteamId = SteamIdJogador, Membros = [SteamIdJogador, "76500000000000456"] }],
        });

        var controller = CriarController(apiKeyNoHeader: ApiKeyValida);

        var resultado = await controller.GetGrupoJogador(SteamIdJogador);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<GrupoJogadorDto>(ok.Value);

        Assert.True(dto.TemGrupo);
        Assert.Equal("Grupo de Fulano", dto.Nome);
    }

    [Fact]
    public async Task GetGrupoJogador_SemGrupo_RetornaTemGrupoFalso()
    {
        var controller = CriarController(apiKeyNoHeader: ApiKeyValida);

        var resultado = await controller.GetGrupoJogador(SteamIdJogador);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<GrupoJogadorDto>(ok.Value);

        Assert.False(dto.TemGrupo);
    }
}
