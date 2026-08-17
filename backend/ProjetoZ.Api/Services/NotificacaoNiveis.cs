namespace ProjetoZ.Api.Services;

public static class NotificacaoNiveis
{
    public const int DiasAteExpirar = 7;

    public static readonly HashSet<string> Validos = new(StringComparer.OrdinalIgnoreCase)
    {
        "verde",
        "amarelo",
        "vermelho",
    };

    public static bool NivelValido(string nivel) => Validos.Contains(nivel);
}
