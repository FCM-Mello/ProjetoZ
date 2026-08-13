using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class CreateSorteioRequest
    {
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Descricao { get; set; } = string.Empty;

        public int? PremioVipNivel { get; set; }

        public List<Guid> PremioProdutoIds { get; set; } = new();
    }
}
