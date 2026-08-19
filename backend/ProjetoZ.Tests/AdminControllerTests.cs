using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
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

    private User CriarUsuarioComum(string? steamId = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = steamId ?? Guid.NewGuid().ToString(), Name = "Jogador" },
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

    [Fact]
    public async Task Banir_TentandoBanirAPropriaConta_RetornaBadRequestENaoAltera()
    {
        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.Banir(admin.Id, new BanirRequest());

        Assert.IsType<BadRequestObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(admin.Id);
        Assert.False(atualizado!.Banido);
    }

    [Fact]
    public async Task Banir_SuperAdminFixo_NuncaPodeSerBanidoNemPorOutroAdmin()
    {
        var superAdmin = CriarAdmin(SuperAdminSteamId);
        var outroAdmin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(outroAdmin.Id);

        var resultado = await controller.Banir(superAdmin.Id, new BanirRequest());

        Assert.IsType<BadRequestObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(superAdmin.Id);
        Assert.False(atualizado!.Banido);
    }

    [Fact]
    public async Task Banir_UsuarioComum_GravaMotivoEData()
    {
        var admin = CriarAdmin();
        var alvo = CriarUsuarioComum();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.Banir(alvo.Id, new BanirRequest { Motivo = "Spam no chat" });

        Assert.IsType<OkObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(alvo.Id);
        Assert.True(atualizado!.Banido);
        Assert.Equal("Spam no chat", atualizado.BanidoMotivo);
        Assert.NotNull(atualizado.BanidoEm);
    }

    [Fact]
    public async Task RemoverBan_UsuarioBanido_LimpaOsCampos()
    {
        var admin = CriarAdmin();
        var alvo = CriarUsuarioComum();
        alvo.Banido = true;
        alvo.BanidoMotivo = "teste";
        alvo.BanidoEm = DateTime.UtcNow;
        await _db.Context.SaveChangesAsync();

        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.RemoverBan(alvo.Id);

        Assert.IsType<OkObjectResult>(resultado);

        _db.Context.ChangeTracker.Clear();
        var atualizado = await _db.Context.Users.FindAsync(alvo.Id);
        Assert.False(atualizado!.Banido);
        Assert.Null(atualizado.BanidoMotivo);
        Assert.Null(atualizado.BanidoEm);
    }

    [Fact]
    public async Task GetUsuario_IncluiSegurosECompras()
    {
        var admin = CriarAdmin();
        var alvo = CriarUsuarioComum();

        _db.Context.Seguros.Add(new Seguro
        {
            Id = Guid.NewGuid(),
            UserId = alvo.Id,
            ItemId = "carro",
            CriadoEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.AddMonths(1),
        });

        _db.Context.Compras.Add(new Compra
        {
            Id = Guid.NewGuid(),
            UserId = alvo.Id,
            Tipo = "produto",
            Descricao = "Item de teste",
            Coins = 100,
            CriadoEm = DateTime.UtcNow,
        });

        await _db.Context.SaveChangesAsync();

        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.GetUsuario(alvo.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var detalhe = Assert.IsType<AdminUsuarioDetalheDto>(ok.Value);

        Assert.Single(detalhe.Seguros);
        Assert.Equal("carro", detalhe.Seguros[0].Id);

        Assert.Single(detalhe.Compras);
        Assert.Equal("Item de teste", detalhe.Compras[0].Descricao);
    }

    private Cla CriarCla(User lider, string nome = "Clã de Teste")
    {
        var cla = new Cla { Id = Guid.NewGuid(), Nome = nome, LiderUserId = lider.Id, LiderSteamId = lider.Profile!.SteamId! };
        _db.Context.Clas.Add(cla);
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = lider.Id, SteamId = lider.Profile.SteamId!, IsAdmin = true });
        _db.Context.SaveChanges();

        return cla;
    }

    [Fact]
    public async Task GetClas_RetornaContagemDeMembrosELiderNome()
    {
        var lider = CriarUsuarioComum();
        var cla = CriarCla(lider, nome: "Legião");

        var membro = CriarUsuarioComum();
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = membro.Id, SteamId = membro.Profile!.SteamId! });
        await _db.Context.SaveChangesAsync();

        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.GetClas();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<IEnumerable<AdminClaDto>>(ok.Value).ToList();

        var dto = Assert.Single(lista);
        Assert.Equal("Legião", dto.Nome);
        Assert.Equal(2, dto.TotalMembros);
        Assert.Equal(lider.Profile!.Name, dto.LiderNome);
    }

    [Fact]
    public async Task GetCla_RetornaMembrosComPapeis()
    {
        var lider = CriarUsuarioComum();
        var cla = CriarCla(lider);

        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.GetCla(cla.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var detalhe = Assert.IsType<AdminClaDetalheDto>(ok.Value);

        var membro = Assert.Single(detalhe.Membros);
        Assert.True(membro.IsLider);
    }

    [Fact]
    public async Task RemoverMembroCla_MembroComum_Remove()
    {
        var lider = CriarUsuarioComum();
        var cla = CriarCla(lider);

        var membro = CriarUsuarioComum();
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = membro.Id, SteamId = membro.Profile!.SteamId! });
        await _db.Context.SaveChangesAsync();

        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.RemoverMembroCla(cla.Id, membro.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(await _db.Context.ClaMembros.AnyAsync(m => m.UserId == membro.Id));
    }

    [Fact]
    public async Task RemoverMembroCla_TentaRemoverOLider_RetornaBadRequest()
    {
        var lider = CriarUsuarioComum();
        var cla = CriarCla(lider);

        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.RemoverMembroCla(cla.Id, lider.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.True(await _db.Context.ClaMembros.AnyAsync(m => m.UserId == lider.Id));
    }

    [Fact]
    public async Task DesfazerCla_RemoveClaEMembros()
    {
        var lider = CriarUsuarioComum();
        var cla = CriarCla(lider);

        var admin = CriarAdmin();
        var controller = new AdminController(_db.Context);
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.DesfazerCla(cla.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(await _db.Context.Clas.AnyAsync());
        Assert.False(await _db.Context.ClaMembros.AnyAsync());
    }
}
