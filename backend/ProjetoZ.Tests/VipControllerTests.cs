using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Tests;

public class VipControllerTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private User CriarUsuario(int coins)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Coins = coins,
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    // ExecuteUpdateAsync não atualiza a entidade rastreada em memória — ela
    // fica com o valor antigo no identity map do próprio DbContext. Em
    // produção isso não importa (cada requisição usa um DbContext novo), mas
    // aqui, no mesmo contexto do teste, é preciso limpar o rastreamento antes
    // de reconsultar para enxergar o valor realmente persistido no banco.
    private async Task<User> RelerDoBanco(Guid id)
    {
        _db.Context.ChangeTracker.Clear();
        return (await _db.Context.Users.FindAsync(id))!;
    }

    [Fact]
    public async Task Comprar_SaldoSuficiente_DebitaEAtivaVip()
    {
        var user = CriarUsuario(coins: 500);
        var controller = new VipController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(1); // Bronze, 300 coins

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var body = ok.Value!;

        var coinsRetornado = (int)body.GetType().GetProperty("coins")!.GetValue(body)!;
        Assert.Equal(200, coinsRetornado);

        var userAtualizado = await RelerDoBanco(user.Id);
        Assert.Equal(200, userAtualizado.Coins);
        Assert.Equal(1, userAtualizado.VipNivel);
        Assert.NotNull(userAtualizado.VipExpiraEm);
        Assert.True(userAtualizado.VipExpiraEm > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task Comprar_SaldoInsuficiente_RetornaBadRequestENaoAlteraNada()
    {
        var user = CriarUsuario(coins: 100);
        var controller = new VipController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(1); // Bronze custa 300

        Assert.IsType<BadRequestObjectResult>(resultado);

        var userAtualizado = await RelerDoBanco(user.Id);
        Assert.Equal(100, userAtualizado.Coins);
        Assert.Equal(0, userAtualizado.VipNivel);
    }

    [Fact]
    public async Task Comprar_NivelInvalido_RetornaNotFound()
    {
        var user = CriarUsuario(coins: 9999);
        var controller = new VipController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(99);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Comprar_ComprasSequenciaisConsomemSaldoCorretamente()
    {
        // Não é um teste real de concorrência (xUnit roda sequencialmente
        // aqui), mas confirma que o UPDATE condicional não permite que uma
        // segunda compra passe depois que o saldo já não é mais suficiente —
        // é exatamente essa checagem no banco que protege contra a corrida
        // em produção quando duas requisições chegam em paralelo.
        var user = CriarUsuario(coins: 300);
        var controller = new VipController(_db.Context);
        controller.ComoUsuario(user.Id);

        var primeira = await controller.Comprar(1); // 300 coins, saldo exato
        Assert.IsType<OkObjectResult>(primeira);

        var segunda = await controller.Comprar(1); // saldo já é 0
        Assert.IsType<BadRequestObjectResult>(segunda);

        var userFinal = await RelerDoBanco(user.Id);
        Assert.Equal(0, userFinal.Coins);
    }
}
