using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProjetoZ.Api.Authorization;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class NotBannedAuthorizationHandlerTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private static ClaimsPrincipal PrincipalPara(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "TesteAutenticado");

        return new ClaimsPrincipal(identity);
    }

    private async Task<bool> Autoriza(Guid userId)
    {
        var handler = new NotBannedAuthorizationHandler(_db.Context);
        var context = new AuthorizationHandlerContext(
            [new NotBannedRequirement()], PrincipalPara(userId), null);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Usuario_NaoBanido_PassaNaAutorizacao()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = "76500000000000321", Name = "Jogador" },
        };

        _db.Context.Users.Add(user);
        await _db.Context.SaveChangesAsync();

        Assert.True(await Autoriza(user.Id));
    }

    [Fact]
    public async Task Usuario_Banido_FalhaNaAutorizacao()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Banido = true,
            Profile = new SteamProfile { SteamId = "76500000000000322", Name = "Jogador" },
        };

        _db.Context.Users.Add(user);
        await _db.Context.SaveChangesAsync();

        Assert.False(await Autoriza(user.Id));
    }

    [Fact]
    public async Task Usuario_Inexistente_FalhaNaAutorizacao()
    {
        Assert.False(await Autoriza(Guid.NewGuid()));
    }
}
