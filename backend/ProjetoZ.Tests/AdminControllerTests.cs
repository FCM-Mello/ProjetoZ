using Microsoft.AspNetCore.Mvc;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class AdminControllerTests : IDisposable
{
    private const string SuperAdminSteamId = "76561198886359962";

    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private User CriarAdmin(string? steamId = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            IsAdmin = true,
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = steamId == null ? null : new SteamProfile { SteamId = steamId, Name = "Admin" },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    [Fact]
    public async Task RemoverAdmin_TentandoRemoverAPropriaConta_RetornaBadRequestENaoAltera()
    {
        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.RemoverAdmin(admin.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(admin.Id);
        Assert.True(atualizado!.IsAdmin);
    }

    [Fact]
    public async Task RemoverAdmin_SuperAdminFixo_NuncaPodeSerRebaixadoNemPorOutroAdmin()
    {
        var superAdmin = CriarAdmin(SuperAdminSteamId);
        var outroAdmin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(outroAdmin.Id);

        var resultado = await controller.RemoverAdmin(superAdmin.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(superAdmin.Id);
        Assert.True(atualizado!.IsAdmin);
    }

    [Fact]
    public async Task RemoverAdmin_OutroAdminComum_PodeSerRebaixado()
    {
        var quemRemove = CriarAdmin();
        var alvo = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(quemRemove.Id);

        var resultado = await controller.RemoverAdmin(alvo.Id);

        Assert.IsType<OkObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(alvo.Id);
        Assert.False(atualizado!.IsAdmin);
    }
}
