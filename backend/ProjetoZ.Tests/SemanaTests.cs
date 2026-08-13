using ProjetoZ.Api.Services;

namespace ProjetoZ.Tests;

public class SemanaTests
{
    [Fact]
    public void InicioSemanaAtualUtc_CaiSempreNumaSegundaFeira()
    {
        var tz = Semana.FusoBrasil();
        var inicioUtc = Semana.InicioSemanaAtualUtc();
        var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(inicioUtc, tz);

        Assert.Equal(DayOfWeek.Monday, inicioLocal.DayOfWeek);
    }

    [Fact]
    public void InicioSemanaAtualUtc_CaiExatamenteNaMeiaNoiteLocal()
    {
        var tz = Semana.FusoBrasil();
        var inicioUtc = Semana.InicioSemanaAtualUtc();
        var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(inicioUtc, tz);

        Assert.Equal(0, inicioLocal.Hour);
        Assert.Equal(0, inicioLocal.Minute);
        Assert.Equal(0, inicioLocal.Second);
    }

    [Fact]
    public void InicioSemanaAtualUtc_NuncaEstaNoFuturo()
    {
        Assert.True(Semana.InicioSemanaAtualUtc() <= DateTime.UtcNow);
    }

    [Fact]
    public void ProximoFechamentoUtc_EExatamenteSeteDiasDepoisDoInicio()
    {
        var inicio = Semana.InicioSemanaAtualUtc();
        var proximoFechamento = Semana.ProximoFechamentoUtc();

        Assert.Equal(inicio.AddDays(7), proximoFechamento);
    }

    [Fact]
    public void ProximoFechamentoUtc_EstaSempreNoFuturo()
    {
        // Como o fechamento semanal roda continuamente, o "próximo fechamento"
        // nunca deveria estar no passado no momento em que é consultado.
        Assert.True(Semana.ProximoFechamentoUtc() > DateTime.UtcNow);
    }
}
