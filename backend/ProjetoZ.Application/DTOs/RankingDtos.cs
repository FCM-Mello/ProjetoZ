namespace ProjetoZ.Application.DTOs
{
    // Sincroniza os totais absolutos de kills/deaths do jogador (o mod manda
    // o total atual, não um incremento) — mesma convenção de
    // SincronizarPosicoesRequest pra veículos.
    public class SincronizarKdRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public int Kills { get; set; }

        public int Deaths { get; set; }
    }

    // Chamado uma vez a cada conclusão do KOTH pelo jogador — soma 1 ao
    // contador, diferente do K/D acima (que é um total absoluto).
    public class RegistrarKothRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
    }

    // Devolvido por GET /api/ranking (site, JWT).
    public class RankingJogadorDto
    {
        public string SteamId { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public double Kd { get; set; }

        public int KothCompletados { get; set; }
    }
}
