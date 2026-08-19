using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class ClasControllerTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private ClasController CriarController() => new(_db.Context);

    private User CriarUsuario(string nome = "Jogador")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
            Profile = new SteamProfile { SteamId = Guid.NewGuid().ToString(), Name = nome },
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    private Cla CriarCla(User lider, string nome = "Clã de Teste")
    {
        var cla = new Cla { Id = Guid.NewGuid(), Nome = nome, LiderUserId = lider.Id, LiderSteamId = lider.Profile!.SteamId! };
        _db.Context.Clas.Add(cla);
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = lider.Id, SteamId = lider.Profile.SteamId!, IsAdmin = true });
        _db.Context.SaveChanges();

        return cla;
    }

    private void AdicionarMembro(Cla cla, User user, bool isAdmin = false)
    {
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = user.Id, SteamId = user.Profile!.SteamId!, IsAdmin = isAdmin });
        _db.Context.SaveChanges();
    }

    [Fact]
    public async Task Criar_UsuarioSemCla_CriaClaEViraLiderAdmin()
    {
        var user = CriarUsuario();
        var controller = CriarController();
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Criar(new CriarClaRequest { Nome = "Os Sobreviventes", Descricao = "desc" });

        Assert.IsType<OkObjectResult>(resultado);

        var cla = await _db.Context.Clas.SingleAsync();
        Assert.Equal(user.Id, cla.LiderUserId);

        var membro = await _db.Context.ClaMembros.SingleAsync();
        Assert.Equal(user.Id, membro.UserId);
        Assert.True(membro.IsAdmin);
    }

    [Fact]
    public async Task Criar_UsuarioJaTemCla_RetornaBadRequest()
    {
        var user = CriarUsuario();
        CriarCla(user);
        var controller = CriarController();
        controller.ComoUsuario(user.Id);

        var resultado = await controller.Criar(new CriarClaRequest { Nome = "Outro Clã" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task Criar_NomeDuplicado_RetornaBadRequest()
    {
        var lider1 = CriarUsuario();
        CriarCla(lider1, nome: "Nome Repetido");

        var user2 = CriarUsuario();
        var controller = CriarController();
        controller.ComoUsuario(user2.Id);

        var resultado = await controller.Criar(new CriarClaRequest { Nome = "Nome Repetido" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task Solicitar_UsuarioSemCla_CriaSolicitacao()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario();
        var controller = CriarController();
        controller.ComoUsuario(candidato.Id);

        var resultado = await controller.Solicitar(cla.Id);

        Assert.IsType<OkResult>(resultado);
        Assert.True(await _db.Context.ClaSolicitacoes.AnyAsync(s => s.ClaId == cla.Id && s.UserId == candidato.Id));
    }

    [Fact]
    public async Task Solicitar_UsuarioJaTemCla_RetornaBadRequest()
    {
        var lider1 = CriarUsuario();
        CriarCla(lider1, nome: "Clã 1");

        var lider2 = CriarUsuario();
        var cla2 = CriarCla(lider2, nome: "Clã 2");

        var controller = CriarController();
        controller.ComoUsuario(lider1.Id);

        var resultado = await controller.Solicitar(cla2.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task Solicitar_JaSolicitouAnteriormente_RetornaBadRequest()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario();
        var controller = CriarController();
        controller.ComoUsuario(candidato.Id);

        await controller.Solicitar(cla.Id);
        var segunda = await controller.Solicitar(cla.Id);

        Assert.IsType<BadRequestObjectResult>(segunda);
    }

    [Fact]
    public async Task AprovarSolicitacao_ComoLider_CriaMembroERemoveSolicitacao()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario();
        var solicitacao = new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id };
        _db.Context.ClaSolicitacoes.Add(solicitacao);
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.AprovarSolicitacao(cla.Id, solicitacao.Id);

        Assert.IsType<OkResult>(resultado);
        Assert.True(await _db.Context.ClaMembros.AnyAsync(m => m.ClaId == cla.Id && m.UserId == candidato.Id));
        Assert.False(await _db.Context.ClaSolicitacoes.AnyAsync(s => s.Id == solicitacao.Id));
    }

    [Fact]
    public async Task AprovarSolicitacao_MembroComumSemAdmin_RetornaForbid()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var membroComum = CriarUsuario();
        AdicionarMembro(cla, membroComum);

        var candidato = CriarUsuario();
        var solicitacao = new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id };
        _db.Context.ClaSolicitacoes.Add(solicitacao);
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(membroComum.Id);

        var resultado = await controller.AprovarSolicitacao(cla.Id, solicitacao.Id);

        Assert.IsType<ForbidResult>(resultado);
    }

    [Fact]
    public async Task RemoverSolicitacao_OProprioAutorCancela()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario();
        var solicitacao = new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id };
        _db.Context.ClaSolicitacoes.Add(solicitacao);
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(candidato.Id);

        var resultado = await controller.RemoverSolicitacao(cla.Id, solicitacao.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(await _db.Context.ClaSolicitacoes.AnyAsync());
    }

    [Fact]
    public async Task PromoverAdmin_AdminComumConsegue()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var admin = CriarUsuario();
        AdicionarMembro(cla, admin, isAdmin: true);

        var membroComum = CriarUsuario();
        AdicionarMembro(cla, membroComum);

        var controller = CriarController();
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.PromoverAdmin(cla.Id, membroComum.Id);

        Assert.IsType<OkResult>(resultado);
        var membro = await _db.Context.ClaMembros.SingleAsync(m => m.UserId == membroComum.Id);
        Assert.True(membro.IsAdmin);
    }

    [Fact]
    public async Task RemoverAdmin_ChamadoPorOutroAdmin_RetornaForbid()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var admin1 = CriarUsuario();
        AdicionarMembro(cla, admin1, isAdmin: true);

        var admin2 = CriarUsuario();
        AdicionarMembro(cla, admin2, isAdmin: true);

        var controller = CriarController();
        controller.ComoUsuario(admin1.Id);

        var resultado = await controller.RemoverAdmin(cla.Id, admin2.Id);

        Assert.IsType<ForbidResult>(resultado);
        Assert.True((await _db.Context.ClaMembros.SingleAsync(m => m.UserId == admin2.Id)).IsAdmin);
    }

    [Fact]
    public async Task RemoverAdmin_ChamadoPeloLider_Funciona()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var admin = CriarUsuario();
        AdicionarMembro(cla, admin, isAdmin: true);

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.RemoverAdmin(cla.Id, admin.Id);

        Assert.IsType<OkResult>(resultado);
        Assert.False((await _db.Context.ClaMembros.SingleAsync(m => m.UserId == admin.Id)).IsAdmin);
    }

    [Fact]
    public async Task Sair_MembroComum_Remove()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var membro = CriarUsuario();
        AdicionarMembro(cla, membro);

        var controller = CriarController();
        controller.ComoUsuario(membro.Id);

        var resultado = await controller.Sair(cla.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(await _db.Context.ClaMembros.AnyAsync(m => m.UserId == membro.Id));
    }

    [Fact]
    public async Task Sair_Lider_RetornaBadRequest()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.Sair(cla.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.True(await _db.Context.ClaMembros.AnyAsync(m => m.UserId == lider.Id));
    }

    [Fact]
    public async Task Desfazer_ChamadoPeloLider_ApagaClaMembrosESolicitacoes()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario();
        _db.Context.ClaSolicitacoes.Add(new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id });
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.Desfazer(cla.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.False(await _db.Context.Clas.AnyAsync());
        Assert.False(await _db.Context.ClaMembros.AnyAsync());
        Assert.False(await _db.Context.ClaSolicitacoes.AnyAsync());
    }

    [Fact]
    public async Task Desfazer_ChamadoPorNaoLider_RetornaForbid()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var admin = CriarUsuario();
        AdicionarMembro(cla, admin, isAdmin: true);

        var controller = CriarController();
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.Desfazer(cla.Id);

        Assert.IsType<ForbidResult>(resultado);
        Assert.True(await _db.Context.Clas.AnyAsync());
    }

    [Fact]
    public async Task GetMeu_UsuarioSemCla_RetornaNoContent()
    {
        var user = CriarUsuario();
        var controller = CriarController();
        controller.ComoUsuario(user.Id);

        var resultado = await controller.GetMeu();

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task GetMeu_LiderDoCla_TrazSolicitacoesPendentes()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var candidato = CriarUsuario(nome: "Candidato");
        _db.Context.ClaSolicitacoes.Add(new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id });
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.GetMeu();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<ClaDetalheDto>(ok.Value);

        Assert.True(dto.SouLider);
        Assert.Single(dto.Solicitacoes);
        Assert.Equal("Candidato", dto.Solicitacoes[0].Nome);
    }

    [Fact]
    public async Task GetPorId_MembroComumNaoVeSolicitacoes()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var membroComum = CriarUsuario();
        AdicionarMembro(cla, membroComum);

        var candidato = CriarUsuario();
        _db.Context.ClaSolicitacoes.Add(new ClaSolicitacao { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = candidato.Id });
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(membroComum.Id);

        var resultado = await controller.GetPorId(cla.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<ClaDetalheDto>(ok.Value);

        Assert.False(dto.SouAdmin);
        Assert.Empty(dto.Solicitacoes);
    }

    [Fact]
    public async Task GetTodos_RetornaContagemDeMembros()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        var membro = CriarUsuario();
        AdicionarMembro(cla, membro);

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.GetTodos();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<List<ClaResumoDto>>(ok.Value);

        var item = Assert.Single(lista);
        Assert.Equal(2, item.TotalMembros);
    }

    [Fact]
    public async Task MembroSincronizadoDoModSemContaNoSite_AparecePorSteamId()
    {
        var lider = CriarUsuario();
        var cla = CriarCla(lider);

        // Membro de origem mod, sem UserId (nunca logou no site).
        _db.Context.ClaMembros.Add(new ClaMembro { Id = Guid.NewGuid(), ClaId = cla.Id, UserId = null, SteamId = "76500000000009999" });
        _db.Context.SaveChanges();

        var controller = CriarController();
        controller.ComoUsuario(lider.Id);

        var resultado = await controller.GetTodos();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<List<ClaResumoDto>>(ok.Value);

        Assert.Equal(2, Assert.Single(lista).TotalMembros);
    }
}
