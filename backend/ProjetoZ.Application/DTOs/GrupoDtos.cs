namespace ProjetoZ.Application.DTOs
{
    // Sync absoluto de todos os grupos ativos no momento da chamada — os que
    // não vierem na lista deixam de existir do lado da API.
    public class GrupoSyncRequest
    {
        public string ApiKey { get; set; } = string.Empty;

        public List<GrupoSyncItemDto> Grupos { get; set; } = new();
    }

    public class GrupoSyncItemDto
    {
        public string Id { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string LiderSteamId { get; set; } = string.Empty;

        public List<string> Membros { get; set; } = new();
    }

    // Devolvido por GET /api/game/grupos/jogador/{steamId} (mod, chave por
    // header). "Sem grupo" é um estado válido — vem 200 com TemGrupo=false,
    // não 404.
    public class GrupoJogadorDto
    {
        public bool TemGrupo { get; set; }

        public string? Id { get; set; }

        public string? Nome { get; set; }

        public string? LiderSteamId { get; set; }

        public List<string>? Membros { get; set; }
    }
}
