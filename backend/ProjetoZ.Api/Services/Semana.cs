namespace ProjetoZ.Api.Services;

// Semana do ranking de clipes: sempre segunda 00h no horário de Brasília.
public static class Semana
{
    public static TimeZoneInfo FusoBrasil()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("BRT-fixo", TimeSpan.FromHours(-3), "Brasília (fixo)", "Brasília (fixo)");
        }
    }

    public static DateTime InicioSemanaAtualUtc()
    {
        var tz = FusoBrasil();
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var diasDesdeSegunda = ((int)agoraLocal.DayOfWeek + 6) % 7;
        var inicioSemanaLocal = agoraLocal.Date.AddDays(-diasDesdeSegunda);

        return TimeZoneInfo.ConvertTimeToUtc(inicioSemanaLocal, tz);
    }

    public static DateTime ProximoFechamentoUtc() => InicioSemanaAtualUtc().AddDays(7);
}
