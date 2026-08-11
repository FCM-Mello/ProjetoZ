namespace ProjetoZ.Application.DTOs
{
    public class CreateSorteioRequest
    {
        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int? PremioVipNivel { get; set; }

        public List<Guid> PremioProdutoIds { get; set; } = new();
    }
}
