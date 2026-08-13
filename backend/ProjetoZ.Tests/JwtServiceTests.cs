using Microsoft.Extensions.Configuration;
using ProjetoZ.Api.Services;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Tests;

public class JwtServiceTests
{
    private static JwtService CriarServico(string chave = "chave-de-teste-bem-longa-pra-hmac-sha256-nao-reclamar")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = chave,
                ["Jwt:Issuer"] = "ProjetoZ-Teste",
                ["Jwt:Audience"] = "ProjetoZ-Teste",
            })
            .Build();

        return new JwtService(config);
    }

    [Fact]
    public void Generate_DepoisValidarEExtrairUserId_RetornaOMesmoId()
    {
        var servico = CriarServico();
        var usuario = new User { Id = Guid.NewGuid() };

        var token = servico.Generate(usuario);
        var idExtraido = servico.ValidarEExtrairUserId(token);

        Assert.Equal(usuario.Id, idExtraido);
    }

    [Fact]
    public void ValidarEExtrairUserId_TokenLixo_RetornaNull()
    {
        var servico = CriarServico();

        Assert.Null(servico.ValidarEExtrairUserId("isso.não.é-um-jwt"));
        Assert.Null(servico.ValidarEExtrairUserId(""));
    }

    [Fact]
    public void ValidarEExtrairUserId_TokenAssinadoComChaveDiferente_RetornaNull()
    {
        var servicoA = CriarServico("chave-A-bem-longa-pra-hmac-sha256-nao-reclamar");
        var servicoB = CriarServico("chave-B-completamente-diferente-tambem-longa");

        var token = servicoA.Generate(new User { Id = Guid.NewGuid() });

        // Um token assinado com uma chave não pode ser validado com outra —
        // é exatamente essa propriedade que protege contra forjar sessão de
        // outro usuário sem conhecer o segredo real.
        Assert.Null(servicoB.ValidarEExtrairUserId(token));
    }

    [Fact]
    public void ValidarEExtrairUserId_TokenComAssinaturaAdulterada_RetornaNull()
    {
        var servico = CriarServico();
        var token = servico.Generate(new User { Id = Guid.NewGuid() });

        var partes = token.Split('.');
        var assinaturaAdulterada = partes[2].Length > 0
            ? (partes[2][0] == 'A' ? 'B' : 'A') + partes[2][1..]
            : "adulterado";

        var tokenAdulterado = $"{partes[0]}.{partes[1]}.{assinaturaAdulterada}";

        Assert.Null(servico.ValidarEExtrairUserId(tokenAdulterado));
    }
}
