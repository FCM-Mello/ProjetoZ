namespace ProjetoZ.Api.Services;

public static class VipTiers
{
    public static readonly Dictionary<int, string> Nomes = new()
    {
        [1] = "Bronze",
        [2] = "Prata",
        [3] = "Ouro",
    };

    public const int DuracaoDias = 30;

    public static string NomeDoNivel(int nivel) =>
        Nomes.TryGetValue(nivel, out var nome) ? nome : "Nenhum";

    // O VIP expira sozinho: não fica ativo até alguém limpar o campo no banco,
    // simplesmente para de valer quando a data passa.
    public static int NivelEfetivo(int nivel, DateTime? expiraEm) =>
        expiraEm.HasValue && expiraEm.Value > DateTime.UtcNow ? nivel : 0;
}
