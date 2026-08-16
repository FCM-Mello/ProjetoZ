using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class SeguroExpiracaoEPosicaoTests : IDisposable
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

    private User CriarUsuario(string steamId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = steamId, Name = "Jogador" },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    private Seguro CriarSeguro(Guid userId, string itemId, DateTime? expiraEm = null, DateTime? criadoEm = null, string? carroId = null)
    {
        var seguro = new Seguro
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            CriadoEm = criadoEm ?? DateTime.UtcNow,
            ExpiraEm = expiraEm ?? DateTime.UtcNow.AddMonths(1),
            CarroId = carroId,
        };

        _db.Context.Seguros.Add(seguro);
        _db.Context.SaveChanges();

        return seguro;
    }

    private async Task<Seguro> RelerSeguro(Guid id)
    {
        _db.Context.ChangeTracker.Clear();
        return (await _db.Context.Seguros.FindAsync(id))!;
    }

    // ---- SincronizarPosicoes ----

    [Fact]
    public async Task SincronizarPosicoes_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = "chave-errada",
            Veiculos = [],
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task SincronizarPosicoes_CarroNovo_VinculaAoSeguroMaisAntigoSemCarroId()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro");
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto
                {
                    CarroId = "2011680-906255",
                    SteamId = SteamIdJogador,
                    Nome = "Land Rover",
                    PosicaoGrid = "045 112",
                    X = 4523.1,
                    Z = 11890.4,
                }
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var atualizados = (int)ok.Value!.GetType().GetProperty("atualizados")!.GetValue(ok.Value)!;
        Assert.Equal(1, atualizados);

        var atualizado = await RelerSeguro(seguro.Id);
        Assert.Equal("2011680-906255", atualizado.CarroId);
        Assert.Equal("Land Rover", atualizado.VeiculoNome);
        Assert.Equal("045 112", atualizado.PosicaoGrid);
        Assert.Equal(4523.1, atualizado.PosicaoX);
        Assert.Equal(11890.4, atualizado.PosicaoZ);
        Assert.NotNull(atualizado.PosicaoAtualizadaEm);
    }

    [Fact]
    public async Task SincronizarPosicoes_CarroJaVinculado_AtualizaPosicaoDoMesmoSeguro()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", carroId: "2011680-906255");
        var controller = CriarGameController();

        await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto
                {
                    CarroId = "2011680-906255",
                    SteamId = SteamIdJogador,
                    Nome = "Land Rover",
                    PosicaoGrid = "050 120",
                    X = 5000,
                    Z = 12000,
                }
            ],
        });

        var atualizado = await RelerSeguro(seguro.Id);
        Assert.Equal(seguro.Id, atualizado.Id);
        Assert.Equal("2011680-906255", atualizado.CarroId);
        Assert.Equal("050 120", atualizado.PosicaoGrid);
        Assert.Equal(1, await _db.Context.Seguros.CountAsync());
    }

    [Fact]
    public async Task SincronizarPosicoes_SteamIdDesconhecido_IgnoraSemQuebrarOResto()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro");
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto { CarroId = "carro-fantasma", SteamId = "76500000000000999", Nome = "X", PosicaoGrid = "000 000", X = 0, Z = 0 },
                new VeiculoPosicaoRequestDto { CarroId = "2011680-906255", SteamId = SteamIdJogador, Nome = "Land Rover", PosicaoGrid = "045 112", X = 4523.1, Z = 11890.4 },
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var atualizados = (int)ok.Value!.GetType().GetProperty("atualizados")!.GetValue(ok.Value)!;
        Assert.Equal(1, atualizados);

        var atualizado = await RelerSeguro(seguro.Id);
        Assert.Equal("2011680-906255", atualizado.CarroId);
    }

    [Fact]
    public async Task SincronizarPosicoes_SemSeguroDisponivelPraVincular_Ignora()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto { CarroId = "2011680-906255", SteamId = SteamIdJogador, Nome = "Land Rover", PosicaoGrid = "045 112", X = 4523.1, Z = 11890.4 },
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var atualizados = (int)ok.Value!.GetType().GetProperty("atualizados")!.GetValue(ok.Value)!;
        Assert.Equal(0, atualizados);
        Assert.Empty(await _db.Context.Seguros.ToListAsync());
    }

    [Fact]
    public async Task SincronizarPosicoes_SeguroExpirado_NaoVincula()
    {
        var user = CriarUsuario(SteamIdJogador);
        CriarSeguro(user.Id, "carro", expiraEm: DateTime.UtcNow.AddDays(-1));
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto { CarroId = "2011680-906255", SteamId = SteamIdJogador, Nome = "Land Rover", PosicaoGrid = "045 112", X = 4523.1, Z = 11890.4 },
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var atualizados = (int)ok.Value!.GetType().GetProperty("atualizados")!.GetValue(ok.Value)!;
        Assert.Equal(0, atualizados);
    }

    [Fact]
    public async Task SincronizarPosicoes_DoisCarrosNovosDoMesmoJogador_VinculaCadaUmAUmSeguroDiferente()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguroMaisAntigo = CriarSeguro(user.Id, "carro", criadoEm: DateTime.UtcNow.AddDays(-5));
        var seguroMaisNovo = CriarSeguro(user.Id, "carro", criadoEm: DateTime.UtcNow.AddDays(-1));
        var controller = CriarGameController();

        var resultado = await controller.SincronizarPosicoes(new SincronizarPosicoesRequest
        {
            ApiKey = ApiKeyValida,
            Veiculos =
            [
                new VeiculoPosicaoRequestDto { CarroId = "carro-A", SteamId = SteamIdJogador, Nome = "Land Rover", PosicaoGrid = "045 112", X = 1, Z = 1 },
                new VeiculoPosicaoRequestDto { CarroId = "carro-B", SteamId = SteamIdJogador, Nome = "Lada", PosicaoGrid = "046 113", X = 2, Z = 2 },
            ],
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var atualizados = (int)ok.Value!.GetType().GetProperty("atualizados")!.GetValue(ok.Value)!;
        Assert.Equal(2, atualizados);

        var antigo = await RelerSeguro(seguroMaisAntigo.Id);
        var novo = await RelerSeguro(seguroMaisNovo.Id);

        // O mais antigo pega o primeiro carro do lote (carro-A); o segundo
        // (carro-B) não pode roubar o mesmo seguro, precisa ir pro outro.
        Assert.Equal("carro-A", antigo.CarroId);
        Assert.Equal("carro-B", novo.CarroId);
    }

    // ---- GetSeguros (mod) ----

    [Fact]
    public async Task GetSeguros_SeguroExpirado_NaoAparece()
    {
        var user = CriarUsuario(SteamIdJogador);
        CriarSeguro(user.Id, "carro", expiraEm: DateTime.UtcNow.AddDays(-1));
        var controller = CriarGameController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguros = Assert.IsAssignableFrom<List<SeguroDto>>(ok.Value);
        Assert.Empty(seguros);
    }

    // ---- ResgatarSeguro (expiração) ----

    [Fact]
    public async Task ResgatarSeguro_SeguroExpirado_RetornaBadRequestMesmoPago()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", expiraEm: DateTime.UtcNow.AddDays(-1));
        var controller = CriarGameController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = true,
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    // ---- CriarSeguro define ExpiraEm ----

    [Fact]
    public async Task CriarSeguro_DefineExpiracaoParaUmMesAPartirDeAgora()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarGameController();

        var antes = DateTime.UtcNow;

        var resultado = await controller.CriarSeguro(new CriarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Id = "carro",
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var idSeguro = (Guid)ok.Value!.GetType().GetProperty("idSeguro")!.GetValue(ok.Value)!;

        var seguro = await _db.Context.Seguros.FindAsync(idSeguro);
        Assert.NotNull(seguro);
        Assert.True(seguro!.ExpiraEm >= antes.AddMonths(1).AddMinutes(-1));
        Assert.True(seguro.ExpiraEm <= DateTime.UtcNow.AddMonths(1).AddMinutes(1));
    }

    // ---- SegurosController (site, JWT) ----

    [Fact]
    public async Task GetMeus_RetornaSoOsSegurosAtivosDoUsuarioLogado()
    {
        var user = CriarUsuario(SteamIdJogador);
        var outro = CriarUsuario("76500000000000888");

        var ativo = CriarSeguro(user.Id, "carro", carroId: "carro-A");
        CriarSeguro(user.Id, "carro", expiraEm: DateTime.UtcNow.AddDays(-1));
        CriarSeguro(outro.Id, "carro", carroId: "carro-do-outro");

        var controller = new SegurosController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.GetMeus();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguros = Assert.IsAssignableFrom<List<SeguroAtivoDto>>(ok.Value);

        var dto = Assert.Single(seguros);
        Assert.Equal(ativo.Id, dto.IdSeguro);
        Assert.Equal("carro-A", dto.CarroId);
    }
}
