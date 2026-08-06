using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products.ToListAsync();

        return Ok(products);
    }

    [HttpPost]
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        bool usuariosComOProduto = await _context.Users
            .AnyAsync(u => u.Inventario.Any(p => p.Id == id));

        if (usuariosComOProduto)
            return BadRequest("Existem usuários  com esse produto.");

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}