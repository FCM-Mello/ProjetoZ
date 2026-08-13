using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class GameControllerTests : IDisposable
{
    private const string ApiKeyValida = "chave-secreta-do-mod";

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

    private User CriarUsuarioVip(string steamId, int vipNivel, DateTime? vipExpiraEm = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            VipNivel = vipNivel,
            VipExpiraEm = vipExpiraEm,
            Profile = new SteamProfile { SteamId = steamId, Name = "Jogador" },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    [Fact]
    public async Task GetPlayer_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.GetPlayer(new PlayerLookupRequest { ApiKey = "chave-errada", SteamId = "123" });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task Comprar_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.Comprar(new PlayerComprarRequest
        {
            ApiKey = "chave-errada",
            SteamId = "123",
            ItemId = "item-1",
            Preco = 10,
        });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task GetVips_ApiKeyErrada_RetornaUnauthorized()
    {
        var controller = CriarController();

        var resultado = await controller.GetVips(new ListaVipsRequest { ApiKey = "chave-errada" });

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task GetVips_RetornaSomenteSteamIdEVipNivel_FiltrandoQuemNaoTemVip()
    {
        CriarUsuarioVip("111", vipNivel: 1);
        CriarUsuarioVip("222", vipNivel: 0);
        // VIP expirado mas ainda não zerado pelo job de limpeza: o endpoint
        // filtra pelo campo bruto VipNivel, então ainda deve aparecer.
        CriarUsuarioVip("333", vipNivel: 2, vipExpiraEm: DateTime.UtcNow.AddDays(-1));

        var controller = CriarController();

        var resultado = await controller.GetVips(new ListaVipsRequest { ApiKey = ApiKeyValida });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var vips = Assert.IsAssignableFrom<List<PlayerVipDto>>(ok.Value);

        Assert.Equal(2, vips.Count);
        Assert.Contains(vips, v => v.SteamId == "111" && v.VipNivel == 1);
        Assert.Contains(vips, v => v.SteamId == "333" && v.VipNivel == 2);
        Assert.DoesNotContain(vips, v => v.SteamId == "222");
    }
}
