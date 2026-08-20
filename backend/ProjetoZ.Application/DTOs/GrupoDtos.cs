namespace ProjetoZ.Application.DTOs
{
    // Adiciona 1 jogador a 1 grupo/clã que JÁ EXISTE — clãs só são criados
    // pelo site agora, então esse endpoint nunca cria um novo, só erra
    // (404) se o Id não corresponder a nenhum. Id é o mesmo valor que
    // GrupoJogadorDto.Id devolve (GrupoModId de um clã antigo de origem
    // mod, ou o Guid interno do clã — que é sempre o caso pra clã novo,
    // já que todos nascem no site).
    public class GrupoAdicionarRequest
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

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
