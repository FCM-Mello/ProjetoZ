using ProjetoZ.Api.Services;

namespace ProjetoZ.Tests;

public class VipTiersTests
{
    [Theory]
    [InlineData(1, "Bronze")]
    [InlineData(2, "Prata")]
    [InlineData(3, "Ouro")]
    public void NomeDoNivel_RetornaNomeCorreto(int nivel, string nomeEsperado)
    {
        Assert.Equal(nomeEsperado, VipTiers.NomeDoNivel(nivel));
    }

    [Fact]
    public void NomeDoNivel_NivelInvalido_RetornaNenhum()
    {
        Assert.Equal("Nenhum", VipTiers.NomeDoNivel(0));
        Assert.Equal("Nenhum", VipTiers.NomeDoNivel(99));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(-1, false)]
    public void NivelValido_RetornaCorreto(int nivel, bool esperado)
    {
        Assert.Equal(esperado, VipTiers.NivelValido(nivel));
    }

    [Fact]
    public void NivelEfetivo_SemExpiracao_RetornaZero()
    {
        Assert.Equal(0, VipTiers.NivelEfetivo(2, null));
    }

    [Fact]
    public void NivelEfetivo_ExpiracaoNoFuturo_RetornaNivel()
    {
        var expiraEm = DateTime.UtcNow.AddDays(5);
        Assert.Equal(2, VipTiers.NivelEfetivo(2, expiraEm));
    }

    [Fact]
    public void NivelEfetivo_ExpiracaoNoPassado_RetornaZero()
    {
        var expiraEm = DateTime.UtcNow.AddDays(-1);
        Assert.Equal(0, VipTiers.NivelEfetivo(3, expiraEm));
    }

    [Fact]
    public void NivelEfetivo_ExpiracaoExatamenteAgora_RetornaZero()
    {
        // A checagem é "> agora", não ">=" — o instante exato de expiração já conta como vencido.
        var agora = DateTime.UtcNow;
        Assert.Equal(0, VipTiers.NivelEfetivo(1, agora));
    }

    [Fact]
    public void Precos_TodosOsNiveisTemPrecoPositivo()
    {
        foreach (var nivel in VipTiers.Nomes.Keys)
        {
            Assert.True(VipTiers.Precos.ContainsKey(nivel), $"Nível {nivel} não tem preço definido.");
            Assert.True(VipTiers.Precos[nivel] > 0, $"Preço do nível {nivel} deveria ser positivo.");
        }
    }

    [Fact]
    public void Precos_CrescemComONivel()
    {
        // Regra de negócio implícita: nível maior custa mais.
        Assert.True(VipTiers.Precos[1] < VipTiers.Precos[2]);
        Assert.True(VipTiers.Precos[2] < VipTiers.Precos[3]);
    }
}
