using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Domian.Models;

namespace ProjetoZ.Tests;

public class NotificacaoTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private User CriarUsuario(string? steamId = null)
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

    private NotificacoesController Controller(Guid comoUsuario)
    {
        var controller = new NotificacoesController(_db.Context);
        controller.ComoUsuario(comoUsuario);
        return controller;
    }

    private static CriarNotificacaoRequest RequestBase(bool paraTodos = true, List<Guid>? destinatarios = null, DateTime? enviarEm = null) => new()
    {
        Titulo = "Manutenção",
        Mensagem = "Servidor vai reiniciar às 22h.",
        Nivel = "amarelo",
        ParaTodos = paraTodos,
        DestinatarioUserIds = destinatarios,
        EnviarEm = enviarEm,
    };

    [Fact]
    public async Task Criar_NivelInvalido_RetornaBadRequest()
    {
        var admin = CriarUsuario();
        var controller = Controller(admin.Id);

        var request = RequestBase();
        request.Nivel = "azul";

        var resultado = await controller.Criar(request);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(await _db.Context.Notificacoes.ToListAsync());
    }

    [Fact]
    public async Task Criar_SemParaTodosESemDestinatarios_RetornaBadRequest()
    {
        var admin = CriarUsuario();
        var controller = Controller(admin.Id);

        var resultado = await controller.Criar(RequestBase(paraTodos: false));

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(await _db.Context.Notificacoes.ToListAsync());
    }

    [Fact]
    public async Task Criar_ParaTodos_QualquerUsuarioVeEmMinhas()
    {
        var admin = CriarUsuario();
        var qualquerUsuario = CriarUsuario();

        var criarController = Controller(admin.Id);
        await criarController.Criar(RequestBase(paraTodos: true));

        var minhasController = Controller(qualquerUsuario.Id);
        var resultado = await minhasController.GetMinhas();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var notificacoes = Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(ok.Value);

        var notificacao = Assert.Single(notificacoes);
        Assert.Equal("Manutenção", notificacao.Titulo);
        Assert.False(notificacao.Lida);
    }

    [Fact]
    public async Task Criar_ParaUsuariosEspecificos_SoDestinatarioVe()
    {
        var admin = CriarUsuario();
        var destinatario = CriarUsuario();
        var outroUsuario = CriarUsuario();

        var criarController = Controller(admin.Id);
        var resultadoCriar = await criarController.Criar(RequestBase(paraTodos: false, destinatarios: [destinatario.Id]));
        Assert.IsType<OkObjectResult>(resultadoCriar);

        var doDestinatario = Assert.IsType<OkObjectResult>(await Controller(destinatario.Id).GetMinhas());
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(doDestinatario.Value));

        var doOutro = Assert.IsType<OkObjectResult>(await Controller(outroUsuario.Id).GetMinhas());
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(doOutro.Value));
    }

    [Fact]
    public async Task GetMinhas_NotificacaoAgendadaParaOFuturo_NaoAparece()
    {
        var admin = CriarUsuario();
        var usuario = CriarUsuario();

        await Controller(admin.Id).Criar(RequestBase(paraTodos: true, enviarEm: DateTime.UtcNow.AddDays(3)));

        var resultado = Assert.IsType<OkObjectResult>(await Controller(usuario.Id).GetMinhas());
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(resultado.Value));
    }

    [Fact]
    public async Task GetMinhas_NotificacaoExpirada_NaoAparece()
    {
        var admin = CriarUsuario();
        var usuario = CriarUsuario();

        _db.Context.Notificacoes.Add(new Notificacao
        {
            Id = Guid.NewGuid(),
            Titulo = "Velha",
            Mensagem = "Já passou",
            Nivel = "verde",
            CriadoEm = DateTime.UtcNow.AddDays(-10),
            CriadoPorUserId = admin.Id,
            EnviarEm = DateTime.UtcNow.AddDays(-8),
            ExpiraEm = DateTime.UtcNow.AddDays(-1),
            ParaTodos = true,
        });
        await _db.Context.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(await Controller(usuario.Id).GetMinhas());
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(resultado.Value));
    }

    [Fact]
    public async Task MarcarLida_RefleteNaProximaListagem()
    {
        var admin = CriarUsuario();
        var usuario = CriarUsuario();

        await Controller(admin.Id).Criar(RequestBase(paraTodos: true));

        var notificacaoId = (await _db.Context.Notificacoes.FirstAsync()).Id;

        var usuarioController = Controller(usuario.Id);
        await usuarioController.MarcarLida(notificacaoId);

        var resultado = Assert.IsType<OkObjectResult>(await usuarioController.GetMinhas());
        var notificacao = Assert.Single(Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(resultado.Value));

        Assert.True(notificacao.Lida);
    }

    [Fact]
    public async Task MarcarTodasLidas_MarcaTodasAsVisiveis()
    {
        var admin = CriarUsuario();
        var usuario = CriarUsuario();

        var criarController = Controller(admin.Id);
        await criarController.Criar(RequestBase(paraTodos: true));
        await criarController.Criar(new CriarNotificacaoRequest
        {
            Titulo = "Segunda",
            Mensagem = "Outra mensagem",
            Nivel = "vermelho",
            ParaTodos = true,
        });

        var usuarioController = Controller(usuario.Id);
        await usuarioController.MarcarTodasLidas();

        var resultado = Assert.IsType<OkObjectResult>(await usuarioController.GetMinhas());
        var notificacoes = Assert.IsAssignableFrom<IEnumerable<NotificacaoDto>>(resultado.Value);

        Assert.Equal(2, notificacoes.Count());
        Assert.All(notificacoes, n => Assert.True(n.Lida));
    }

    [Fact]
    public async Task GetTodas_ListaHistoricoCompletoComContagens()
    {
        var admin = CriarUsuario();
        var destinatario = CriarUsuario();

        var criarController = Controller(admin.Id);
        await criarController.Criar(RequestBase(paraTodos: false, destinatarios: [destinatario.Id]));

        var notificacaoId = (await _db.Context.Notificacoes.FirstAsync()).Id;
        await Controller(destinatario.Id).MarcarLida(notificacaoId);

        var resultado = Assert.IsType<OkObjectResult>(await criarController.GetTodas());
        var notificacoes = Assert.IsAssignableFrom<IEnumerable<NotificacaoAdminDto>>(resultado.Value);

        var dto = Assert.Single(notificacoes);
        Assert.False(dto.ParaTodos);
        Assert.Equal(1, dto.TotalDestinatarios);
        Assert.Equal(1, dto.TotalLeituras);
    }

    [Fact]
    public async Task Excluir_RemoveNotificacaoEAssociacoes()
    {
        var admin = CriarUsuario();
        var destinatario = CriarUsuario();

        var criarController = Controller(admin.Id);
        await criarController.Criar(RequestBase(paraTodos: false, destinatarios: [destinatario.Id]));

        var notificacaoId = (await _db.Context.Notificacoes.FirstAsync()).Id;
        await Controller(destinatario.Id).MarcarLida(notificacaoId);

        var resultado = await criarController.Excluir(notificacaoId);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Empty(await _db.Context.Notificacoes.ToListAsync());
        Assert.Empty(await _db.Context.NotificacaoDestinatarios.ToListAsync());
        Assert.Empty(await _db.Context.NotificacaoLeituras.ToListAsync());
    }

    [Fact]
    public async Task Excluir_Inexistente_RetornaNotFound()
    {
        var admin = CriarUsuario();
        var resultado = await Controller(admin.Id).Excluir(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(resultado);
    }
}
