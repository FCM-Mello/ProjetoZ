using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class RankingTests : IDisposable
{
    private const string ApiKeyValida = "chave-secreta-do-mod";
    private const string SteamIdJogador = "76500000000000123";

    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private GameController CriarGameController()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GameServer:ApiKey"] = ApiKeyValida,
            })
            .Build();

        return new GameController(_db.Context, config);
    }

    private User CriarUsuario(string? steamId = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = steamId ?? Guid.NewGuid().ToString(), Name = "Jogador" },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    // ---- RankingCalculos ----

    [Theory]
    [InlineData(10, 2, 5.0)]
    [InlineData(0, 0, 0.0)]
    [InlineData(7, 0, 7.0)]
    public void CalcularKd_CalculaCorretamenteInclusiveComZeroMortes(int kills, int deaths, double esperado)
    {
        Assert.Equal(esperado, RankingCalculos.CalcularKd(kills, deaths));
    }

    // ---- SincronizarKd ----

    [Fact]
    public async Task SincronizarKd_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarGameController();

        var resultado = await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
            Kills = 10,
            Deaths = 2,
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task SincronizarKd_JogadorInexistente_RetornaNotFound()
    {
        var controller = CriarGameController();

        var resultado = await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = "76500000000000999",
            Kills = 10,
            Deaths = 2,
        });

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task SincronizarKd_ValoresNegativos_RetornaBadRequest()
    {
        CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        var resultado = await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Kills = -1,
            Deaths = 2,
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task SincronizarKd_PrimeiraVez_CriaRanking()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        var resultado = await controller.SincronizarKd(new SincronizarKdRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Kills = 15,
            Deaths = 3,
        });

        Assert.IsType<OkResult>(resultado);

        var ranking = await _db.Context.PlayerRankings.SingleAsync(r => r.UserId == user.Id);
        Assert.Equal(15, ranking.Kills);
        Assert.Equal(3, ranking.Deaths);
    }

    [Fact]
    public async Task SincronizarKd_SegundaVez_SubstituiValoresAbsolutosSemSomar()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        await controller.SincronizarKd(new SincronizarKdRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador, Kills = 15, Deaths = 3 });
        await controller.SincronizarKd(new SincronizarKdRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador, Kills = 20, Deaths = 4 });

        var rankings = await _db.Context.PlayerRankings.Where(r => r.UserId == user.Id).ToListAsync();
        var ranking = Assert.Single(rankings);
        Assert.Equal(20, ranking.Kills);
        Assert.Equal(4, ranking.Deaths);
    }

    // ---- RegistrarKoth ----

    [Fact]
    public async Task RegistrarKoth_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarGameController();

        var resultado = await controller.RegistrarKoth(new RegistrarKothRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task RegistrarKoth_PrimeiraVez_CriaComUm()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        var resultado = await controller.RegistrarKoth(new RegistrarKothRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        Assert.IsType<OkObjectResult>(resultado);

        var ranking = await _db.Context.PlayerRankings.SingleAsync(r => r.UserId == user.Id);
        Assert.Equal(1, ranking.KothCompletados);
    }

    [Fact]
    public async Task RegistrarKoth_VariasVezes_Incrementa()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        await controller.RegistrarKoth(new RegistrarKothRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });
        await controller.RegistrarKoth(new RegistrarKothRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });
        await controller.RegistrarKoth(new RegistrarKothRequest { ApiKey = ApiKeyValida, SteamId = SteamIdJogador });

        var ranking = await _db.Context.PlayerRankings.SingleAsync(r => r.UserId == user.Id);
        Assert.Equal(3, ranking.KothCompletados);
    }

    // ---- GetRanking (site) ----

    [Fact]
    public async Task GetRanking_OrdenaPorKdDecrescente()
    {
        var baixo = CriarUsuario();
        var alto = CriarUsuario();

        _db.Context.PlayerRankings.Add(new PlayerRanking { Id = Guid.NewGuid(), UserId = baixo.Id, Kills = 5, Deaths = 5 });
        _db.Context.PlayerRankings.Add(new PlayerRanking { Id = Guid.NewGuid(), UserId = alto.Id, Kills = 20, Deaths = 2 });
        await _db.Context.SaveChangesAsync();

        var controller = new RankingController(_db.Context);

        var resultado = await controller.GetRanking();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<List<RankingJogadorDto>>(ok.Value);

        Assert.Equal(2, lista.Count);
        Assert.Equal(alto.Profile!.SteamId, lista[0].SteamId);
        Assert.True(lista[0].Kd > lista[1].Kd);
    }

    [Fact]
    public async Task ResetarRanking_LimpaTodosOsRegistros()
    {
        var user = CriarUsuario();
        _db.Context.PlayerRankings.Add(new PlayerRanking { Id = Guid.NewGuid(), UserId = user.Id, Kills = 10, Deaths = 1, KothCompletados = 2 });
        await _db.Context.SaveChangesAsync();

        var controller = new RankingController(_db.Context);

        var resultado = await controller.ResetarRanking();

        Assert.IsType<NoContentResult>(resultado);
        Assert.Empty(await _db.Context.PlayerRankings.ToListAsync());
    }
}
