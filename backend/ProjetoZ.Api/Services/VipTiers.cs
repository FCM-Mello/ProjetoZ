namespace ProjetoZ.Api.Services;

public static class VipTiers
{
    public static readonly Dictionary<int, string> Nomes = new()
    {
        [1] = "Bronze",
        [2] = "Prata",
        [3] = "Ouro",
    };

    public static readonly Dictionary<int, int> Precos = new()
    {
        [1] = 300,
        [2] = 600,
        [3] = 1000,
    };

    public const int DuracaoDias = 30;

    public static string NomeDoNivel(int nivel) =>
        Nomes.TryGetValue(nivel, out var nome) ? nome : "Nenhum";

    public static bool NivelValido(int nivel) => Nomes.ContainsKey(nivel);

    // O VIP expira sozinho: não fica ativo até alguém limpar o campo no banco,
    // simplesmente para de valer quando a data passa.
    public static int NivelEfetivo(int nivel, DateTime? expiraEm) =>
        expiraEm.HasValue && expiraEm.Value > DateTime.UtcNow ? nivel : 0;
}
