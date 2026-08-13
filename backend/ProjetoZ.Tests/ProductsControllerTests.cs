using Microsoft.AspNetCore.Mvc;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Tests;

public class ProductsControllerTests : IDisposable
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

    private Product CriarProduto(decimal preco)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Nome = "Item de teste",
            Preco = preco,
            Imagem = "img.png",
            Descricao = "Descrição",
            Estoque = 10,
        };

        _db.Context.Products.Add(product);
        _db.Context.SaveChanges();

        return product;
    }

    private async Task<User> RelerUsuario(Guid id)
    {
        _db.Context.ChangeTracker.Clear();
        return (await _db.Context.Users.FindAsync(id))!;
    }

    [Fact]
    public async Task Comprar_SaldoSuficiente_DebitaEAdicionaAoInventario()
    {
        var user = CriarUsuario(coins: 100);
        var produto = CriarProduto(preco: 40);
        var controller = new ProductsController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(produto.Id);

        Assert.IsType<OkObjectResult>(resultado);

        var userAtualizado = await RelerUsuario(user.Id);
        Assert.Equal(60, userAtualizado.Coins);
        Assert.Contains(produto.Id, userAtualizado.Inventario);
    }

    [Fact]
    public async Task Comprar_SaldoInsuficiente_RetornaBadRequestENaoAlteraInventario()
    {
        var user = CriarUsuario(coins: 10);
        var produto = CriarProduto(preco: 40);
        var controller = new ProductsController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(produto.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);

        var userAtualizado = await RelerUsuario(user.Id);
        Assert.Equal(10, userAtualizado.Coins);
        Assert.Empty(userAtualizado.Inventario);
    }

    [Fact]
    public async Task Comprar_ProdutoInexistente_RetornaNotFound()
    {
        var user = CriarUsuario(coins: 9999);
        var controller = new ProductsController(_db.Context);
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Comprar(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Delete_ProdutoEmInventarioDeAlguemUsuario_RetornaBadRequestENaoRemove()
    {
        var user = CriarUsuario(coins: 0);
        var produto = CriarProduto(preco: 40);
        user.Inventario.Add(produto.Id);
        await _db.Context.SaveChangesAsync();

        var controller = new ProductsController(_db.Context);

        var resultado = await controller.Delete(produto.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.NotNull(await _db.Context.Products.FindAsync(produto.Id));
    }

    [Fact]
    public async Task Delete_ProdutoSemDono_RemoveComSucesso()
    {
        var produto = CriarProduto(preco: 40);
        var controller = new ProductsController(_db.Context);

        var resultado = await controller.Delete(produto.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Null(await _db.Context.Products.FindAsync(produto.Id));
    }
}
