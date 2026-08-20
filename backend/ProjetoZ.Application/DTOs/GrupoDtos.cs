namespace ProjetoZ.Application.DTOs
{
    // Adiciona 1 jogador a 1 grupo — cria o clã na primeira chamada desse
    // Id (grupo novo no jogo). Nome/LiderSteamId vêm sempre atualizados
    // (o mod manda o estado atual a cada chamada, não só na criação).
    public class GrupoAdicionarRequest
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string LiderSteamId { get; set; } = string.Empty;

        public string SteamId { get; set; } = string.Empty;
    }

    // Devolvido por POST /api/game/grupos/jogador (mod). "Sem grupo" é um
    // estado válido — vem 200 com TemGrupo=false, não 404.
    public class GrupoJogadorDto
    {
        public bool TemGrupo { get; set; }

        public string? Id { get; set; }

        public string? Nome { get; set; }

        public string? LiderSteamId { get; set; }

        public List<string>? Membros { get; set; }
    }
}
