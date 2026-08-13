using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Persistence;

namespace ProjetoZ.Tests;

// Banco SQLite em memória, com schema criado do zero — cada teste que usar
// isso ganha um banco limpo e isolado (instância de classe de teste nova a
// cada [Fact], convenção padrão do xUnit).
public sealed class SqliteInMemoryContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext Context { get; }

    public SqliteInMemoryContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public static class TestHelpers
{
    // Simula o HttpContext.User que o [Authorize]/ClaimTypes.NameIdentifier
    // dependem em runtime, sem precisar de um servidor HTTP de verdade.
    public static void ComoUsuario(this ControllerBase controller, Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "TesteAutenticado");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
