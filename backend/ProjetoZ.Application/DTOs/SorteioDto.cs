namespace ProjetoZ.Application.DTOs
{
    public class SorteioDto
    {
        public Guid Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int? PremioVipNivel { get; set; }

        public string? PremioVipNivelNome { get; set; }

        public List<SorteioProdutoDto> PremioProdutos { get; set; } = new();

        public string Status { get; set; } = string.Empty;

        public int TotalParticipantes { get; set; }

        public bool JaParticipando { get; set; }

        public string? VencedorNome { get; set; }

        public DateTime CriadoEm { get; set; }

        public DateTime? SorteadoEm { get; set; }
    }

    public class SorteioProdutoDto
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Imagem { get; set; } = string.Empty;
    }
}
