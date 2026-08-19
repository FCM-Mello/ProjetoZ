namespace ProjetoZ.Application.DTOs
{
    // Sincroniza os totais absolutos do jogador (o mod manda o total atual,
    // não um incremento) — mesma convenção de SincronizarPosicoesRequest pra
    // veículos. KothCompletados aqui é redundante com o que
    // POST ranking/koth já incrementa — serve pra corrigir qualquer desvio
    // caso alguma chamada de incremento tenha se perdido.
    public class SincronizarKdRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public int ZumbiKills { get; set; }

        public int KothCompletados { get; set; }

        public int SegundosJogados { get; set; }
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

    // Devolvido por GET /api/game/ranking/jogador/{steamId} (mod, chave por
    // header) — resumo enxuto de 1 jogador, diferente de RankingJogadorDto
    // (lista completa pro site).
    public class JogadorRankingDto
    {
        public string SteamId { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public int ZumbiKills { get; set; }

        public int KothCompletados { get; set; }

        public int SegundosJogados { get; set; }
    }
}
