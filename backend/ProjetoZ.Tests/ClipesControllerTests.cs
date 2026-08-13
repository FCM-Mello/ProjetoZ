using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Controllers;
using ProjetoZ.Api.Services;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Tests;

public class ClipesControllerTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public void Dispose() => _db.Dispose();

    private static YoutubeService CriarYoutubeService() =>
        new(new HttpClient(), new ConfigurationBuilder().Build());

    private User CriarUsuario(bool isAdmin = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            IsAdmin = isAdmin,
            CriadoEm = DateTime.UtcNow,
            UltimoLogin = DateTime.UtcNow,
        };

        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();

        return user;
    }

    private Clipe CriarClipe(Guid autorId)
    {
        var clipe = new Clipe
        {
            Id = Guid.NewGuid(),
            UserId = autorId,
            Titulo = "Clipe de teste ArkZ",
            Url = "https://youtu.be/dQw4w9WgXcQ",
            CriadoEm = DateTime.UtcNow,
        };

        _db.Context.Clipes.Add(clipe);
        _db.Context.SaveChanges();

        return clipe;
    }

    [Fact]
    public async Task Curtir_ProprioClipe_RetornaBadRequestENaoRegistraCurtida()
    {
        var autor = CriarUsuario();
        var clipe = CriarClipe(autor.Id);
        var controller = new ClipesController(_db.Context, CriarYoutubeService());
        controller.ComoUsuario(autor.Id);

        var resultado = await controller.Curtir(clipe.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Equal(0, _db.Context.ClipeCurtidas.Count());
    }

    [Fact]
    public async Task Curtir_DuasVezes_EIdempotenteContaUmaCurtidaSo()
    {
        var autor = CriarUsuario();
        var quemCurte = CriarUsuario();
        var clipe = CriarClipe(autor.Id);
        var controller = new ClipesController(_db.Context, CriarYoutubeService());
        controller.ComoUsuario(quemCurte.Id);

        await controller.Curtir(clipe.Id);
        var segundaResposta = await controller.Curtir(clipe.Id);

        var ok = Assert.IsType<OkObjectResult>(segundaResposta);
        var curtidas = (int)ok.Value!.GetType().GetProperty("curtidas")!.GetValue(ok.Value)!;

        Assert.Equal(1, curtidas);
        Assert.Equal(1, _db.Context.ClipeCurtidas.Count());
    }

    [Fact]
    public async Task Delete_PeloProprioAutor_RemoveComSucesso()
    {
        var autor = CriarUsuario();
        var clipe = CriarClipe(autor.Id);
        var controller = new ClipesController(_db.Context, CriarYoutubeService());
        controller.ComoUsuario(autor.Id);

        var resultado = await controller.Delete(clipe.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Null(await _db.Context.Clipes.FindAsync(clipe.Id));
    }

    [Fact]
    public async Task Delete_PorUsuarioNaoRelacionado_RetornaForbid()
    {
        var autor = CriarUsuario();
        var outroUsuario = CriarUsuario();
        var clipe = CriarClipe(autor.Id);
        var controller = new ClipesController(_db.Context, CriarYoutubeService());
        controller.ComoUsuario(outroUsuario.Id);

        var resultado = await controller.Delete(clipe.Id);

        Assert.IsType<ForbidResult>(resultado);
        Assert.NotNull(await _db.Context.Clipes.FindAsync(clipe.Id));
    }

    [Fact]
    public async Task Delete_PorAdmin_RemoveMesmoSemSerOAutor()
    {
        var autor = CriarUsuario();
        var admin = CriarUsuario(isAdmin: true);
        var clipe = CriarClipe(autor.Id);
        var controller = new ClipesController(_db.Context, CriarYoutubeService());
        controller.ComoUsuario(admin.Id);

        var resultado = await controller.Delete(clipe.Id);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Null(await _db.Context.Clipes.FindAsync(clipe.Id));
    }
}
