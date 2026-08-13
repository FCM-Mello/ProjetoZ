using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;

namespace ProjetoZ.Api.Controllers
{
    [ApiController]
    [Route("api/category")]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categorys = await _context.Categorys.ToListAsync();

            return Ok(categorys);
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create(CreateCategoryRequest request)
        {
            var nomeJaExiste = await _context.Categorys
                .AnyAsync(c => c.Nome == request.Nome);

            if (nomeJaExiste)
                return BadRequest("Já existe uma categoria com esse nome.");

            var category = new Category
            {
                Nome = request.Nome
            };

            _context.Categorys.Add(category);

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var category = await _context.Categorys.FindAsync(id);

            if (category == null)
                return NotFound();

            bool possuiProdutos = await _context.Products
                .AnyAsync(p => p.Categoria == id);

            if (possuiProdutos)
                return BadRequest("Existem produtos utilizando esta categoria.");

            _context.Categorys.Remove(category);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
