namespace ProjetoZ.Api.Services;

public static class RankingCalculos
{
    // Sem mortes, o K/D é o próprio número de kills (em vez de dividir por
    // zero) — um jogador "invicto" fica acima de quem tem o mesmo número de
    // kills mas já morreu alguma vez.
    public static double CalcularKd(int kills, int deaths) =>
        deaths == 0 ? kills : (double)kills / deaths;
}
