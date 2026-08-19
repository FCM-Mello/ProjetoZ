namespace ProjetoZ.Application.DTOs
{
    public class AdminClaDto
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string? Estandarte { get; set; }

        // Não nulo quando o clã veio do mod (sincronizado como "Grupo") —
        // usado pelo painel pra distinguir de um clã criado no site.
        public string? GrupoModId { get; set; }

        public string LiderNome { get; set; } = string.Empty;

        public int TotalMembros { get; set; }

        public DateTime CriadoEm { get; set; }
    }

    public class AdminClaMembroDto
    {
        // Nulo se o membro veio de sync do mod e nunca fez login no site.
        public Guid? UserId { get; set; }

        public string SteamId { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public bool IsLider { get; set; }

        public bool IsAdmin { get; set; }
    }

    public class AdminClaDetalheDto : AdminClaDto
    {
        public List<AdminClaMembroDto> Membros { get; set; } = new();
    }
}
