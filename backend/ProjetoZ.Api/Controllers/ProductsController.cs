using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("/api/productsPost")]
    public async Task<IActionResult> GetAll([FromBody] string token)
    {
        var products = await _context.Products.ToListAsync();

        return Ok(products);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products.ToListAsync();

        return Ok(products);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var product = new Product
        {
            Nome = request.Nome,
            Preco = request.Preco,
            Imagem = request.Imagem,
            Descricao = request.Descricao,
            Estoque = request.Estoque,
            Categoria = request.Categoria,
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        product.Nome = request.Nome;
        product.Preco = request.Preco;
        product.Imagem = request.Imagem;
        product.Descricao = request.Descricao;
        product.Estoque = request.Estoque;
        product.Categoria = request.Categoria;

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        bool usuariosComOProduto = await _context.Users
            .AnyAsync(u => u.Inventario.Any(p => p == id));

        if (usuariosComOProduto)
            return BadRequest("Existem usuários  com esse produto.");

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/comprar")]
    [Authorize]
    public async Task<IActionResult> Comprar(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));

        if (user == null)
            return Unauthorized();

        var preco = (int)product.Preco;

        if (user.Coins < preco)
            return BadRequest("Saldo de Az Coins insuficiente.");

        user.Coins -= preco;
        user.Inventario = [.. user.Inventario, product.Id];

        _context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Tipo = "produto",
            Descricao = product.Nome,
            Coins = preco,
        });

        await _context.SaveChangesAsync();

        return Ok(new { coins = user.Coins });
    }
}