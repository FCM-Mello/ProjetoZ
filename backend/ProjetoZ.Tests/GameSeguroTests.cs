using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class GameSeguroTests : IDisposable
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

    private Seguro CriarSeguro(Guid userId, string itemId, DateTime? ultimoResgate)
    {
        var seguro = new Seguro
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            CriadoEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.AddMonths(1),
            UltimoResgate = ultimoResgate,
        };

        _db.Context.Seguros.Add(seguro);
        _db.Context.SaveChanges();

        return seguro;
    }

    private static Guid IdSeguroDaResposta(IActionResult resultado)
    {
        var ok = Assert.IsType<OkObjectResult>(resultado);
        return (Guid)ok.Value!.GetType().GetProperty("idSeguro")!.GetValue(ok.Value)!;
    }

    [Fact]
    public async Task CriarSeguro_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.CriarSeguro(new CriarSeguroRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
            Id = "carro",
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task GetSeguros_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task ResgatarSeguro_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
            IdSeguro = Guid.NewGuid(),
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task CriarSeguro_JogadorInexistente_RetornaNotFound()
    {
        var controller = CriarController();

        var resultado = await controller.CriarSeguro(new CriarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = "76500000000000999",
            Id = "carro",
        });

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task CriarSeguro_SemIdDoItem_RetornaBadRequest()
    {
        CriarUsuario(SteamIdJogador);
        var controller = CriarController();

        var resultado = await controller.CriarSeguro(new CriarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Id = "   ",
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CriarSeguro_DadosValidos_GravaEDevolveIdSeguro()
    {
        var user = CriarUsuario(SteamIdJogador);
        var controller = CriarController();

        var resultado = await controller.CriarSeguro(new CriarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Id = "carro",
        });

        var idSeguro = IdSeguroDaResposta(resultado);

        var gravado = await _db.Context.Seguros.FindAsync(idSeguro);
        Assert.NotNull(gravado);
        Assert.Equal(user.Id, gravado!.UserId);
        Assert.Equal("carro", gravado.ItemId);
        Assert.Null(gravado.UltimoResgate);
    }

    [Fact]
    public async Task CriarSeguro_MesmoItemDuasVezes_CriaDoisSegurosIndependentes()
    {
        CriarUsuario(SteamIdJogador);
        var controller = CriarController();

        var pedido = new CriarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            Id = "carro",
        };

        var primeiro = IdSeguroDaResposta(await controller.CriarSeguro(pedido));
        var segundo = IdSeguroDaResposta(await controller.CriarSeguro(pedido));

        Assert.NotEqual(primeiro, segundo);
        Assert.Equal(2, await _db.Context.Seguros.CountAsync());
    }

    [Fact]
    public async Task GetSeguros_SeguroNuncaResgatado_PodeResgatar()
    {
        var user = CriarUsuario(SteamIdJogador);
        CriarSeguro(user.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguros = Assert.IsAssignableFrom<List<SeguroDto>>(ok.Value);

        var seguro = Assert.Single(seguros);
        Assert.Equal("carro", seguro.Id);
        Assert.True(seguro.PodeResgatar);
        Assert.Null(seguro.ProximoResgateEm);
    }

    [Fact]
    public async Task GetSeguros_ResgatadoHaMenosDe48h_NaoPodeResgatarEInformaQuando()
    {
        var user = CriarUsuario(SteamIdJogador);
        var resgatadoEm = DateTime.UtcNow.AddHours(-10);
        CriarSeguro(user.Id, "carro", ultimoResgate: resgatadoEm);
        var controller = CriarController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguro = Assert.Single(Assert.IsAssignableFrom<List<SeguroDto>>(ok.Value));

        Assert.False(seguro.PodeResgatar);
        Assert.NotNull(seguro.ProximoResgateEm);
        Assert.Equal(resgatadoEm.AddHours(48), seguro.ProximoResgateEm!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetSeguros_ResgatadoHaMaisDe48h_PodeResgatarDeNovo()
    {
        var user = CriarUsuario(SteamIdJogador);
        CriarSeguro(user.Id, "carro", ultimoResgate: DateTime.UtcNow.AddHours(-49));
        var controller = CriarController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguro = Assert.Single(Assert.IsAssignableFrom<List<SeguroDto>>(ok.Value));

        Assert.True(seguro.PodeResgatar);
    }

    [Fact]
    public async Task GetSeguros_NaoVazaSeguroDeOutroJogador()
    {
        var user = CriarUsuario(SteamIdJogador);
        var outro = CriarUsuario("76500000000000888");
        CriarSeguro(user.Id, "carro", ultimoResgate: null);
        CriarSeguro(outro.Id, "helicoptero", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.GetSeguros(new ListaSegurosRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var seguros = Assert.IsAssignableFrom<List<SeguroDto>>(ok.Value);

        Assert.Equal("carro", Assert.Single(seguros).Id);
    }

    [Fact]
    public async Task ResgatarSeguro_NuncaResgatado_MarcaDataERegistraNoHistorico()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
        });

        Assert.IsType<OkObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();

        var atualizado = await _db.Context.Seguros.FindAsync(seguro.Id);
        Assert.NotNull(atualizado!.UltimoResgate);

        var compra = Assert.Single(await _db.Context.Compras.ToListAsync());
        Assert.Equal("seguro", compra.Tipo);
        Assert.Equal(0, compra.Coins);
        Assert.Contains("carro", compra.Descricao);
    }

    [Fact]
    public async Task ResgatarSeguro_DentroDoCooldown_RetornaBadRequestENaoAlteraData()
    {
        var user = CriarUsuario(SteamIdJogador);
        var resgatadoEm = DateTime.UtcNow.AddHours(-10);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: resgatadoEm);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
        });

        Assert.IsType<BadRequestObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();

        var atualizado = await _db.Context.Seguros.FindAsync(seguro.Id);
        Assert.Equal(resgatadoEm, atualizado!.UltimoResgate!.Value, TimeSpan.FromSeconds(1));
        Assert.Empty(await _db.Context.Compras.ToListAsync());
    }

    [Fact]
    public async Task ResgatarSeguro_DuasVezesSeguidas_SegundaEBloqueada()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var pedido = new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
        };

        Assert.IsType<OkObjectResult>(await controller.ResgatarSeguro(pedido));
        Assert.IsType<BadRequestObjectResult>(await controller.ResgatarSeguro(pedido));

        Assert.Single(await _db.Context.Compras.ToListAsync());
    }

    [Fact]
    public async Task ResgatarSeguro_SeguroDeOutroJogador_RetornaNotFoundENaoResgata()
    {
        CriarUsuario(SteamIdJogador);
        var outro = CriarUsuario("76500000000000888");
        var seguroDoOutro = CriarSeguro(outro.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguroDoOutro.Id,
        });

        Assert.IsType<NotFoundResult>(resultado);

        _db.Context.ChangeTracker.Clear();

        var intacto = await _db.Context.Seguros.FindAsync(seguroDoOutro.Id);
        Assert.Null(intacto!.UltimoResgate);
    }

    [Fact]
    public async Task ResgatarSeguro_PagoDentroDoCooldown_ResgataEReiniciaOCooldown()
    {
        var user = CriarUsuario(SteamIdJogador);
        var resgatadoEm = DateTime.UtcNow.AddHours(-10);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: resgatadoEm);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = true,
        });

        Assert.IsType<OkObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();

        // O cooldown volta a contar a partir de agora, não da data antiga.
        var atualizado = await _db.Context.Seguros.FindAsync(seguro.Id);
        Assert.Equal(DateTime.UtcNow, atualizado!.UltimoResgate!.Value, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResgatarSeguro_Pago_RegistraComoExpressoSemCobrarCoinsDeNovo()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: DateTime.UtcNow.AddHours(-1));
        var controller = CriarController();

        await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = true,
        });

        var compra = Assert.Single(await _db.Context.Compras.ToListAsync());
        Assert.Equal("seguro", compra.Tipo);
        // O débito real já foi registrado pelo /comprar (Tipo = "mod"); aqui
        // fica 0 pra não contar os mesmos coins duas vezes no histórico.
        Assert.Equal(0, compra.Coins);
        Assert.Contains("expresso", compra.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResgatarSeguro_PagoNaoBurlaDonoDoSeguro()
    {
        CriarUsuario(SteamIdJogador);
        var outro = CriarUsuario("76500000000000888");
        var seguroDoOutro = CriarSeguro(outro.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguroDoOutro.Id,
            Pago = true,
        });

        Assert.IsType<NotFoundResult>(resultado);

        _db.Context.ChangeTracker.Clear();

        var intacto = await _db.Context.Seguros.FindAsync(seguroDoOutro.Id);
        Assert.Null(intacto!.UltimoResgate);
        Assert.Empty(await _db.Context.Compras.ToListAsync());
    }

    [Fact]
    public async Task ResgatarSeguro_PagoFalse_MantemBloqueioDeCooldown()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: DateTime.UtcNow.AddHours(-10));
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = false,
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task ResgatarSeguro_PagoApiKeyErrada_ContinuaUnauthorized()
    {
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var resultado = await controller.ResgatarSeguro(new ResgatarSeguroRequest
        {
            ApiKey = "chave-errada",
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = true,
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task ResgatarSeguro_PagoDuasVezesSeguidas_AmbasPassam()
    {
        // Sem cooldown pra segurar, cada resgate expresso pago vale — é o mod
        // que garante que só chega aqui depois de um débito confirmado.
        var user = CriarUsuario(SteamIdJogador);
        var seguro = CriarSeguro(user.Id, "carro", ultimoResgate: null);
        var controller = CriarController();

        var pedido = new ResgatarSeguroRequest
        {
            ApiKey = ApiKeyValida,
            SteamId = SteamIdJogador,
            IdSeguro = seguro.Id,
            Pago = true,
        };

        Assert.IsType<OkObjectResult>(await controller.ResgatarSeguro(pedido));
        Assert.IsType<OkObjectResult>(await controller.ResgatarSeguro(pedido));

        Assert.Equal(2, (await _db.Context.Compras.ToListAsync()).Count);
    }
}
