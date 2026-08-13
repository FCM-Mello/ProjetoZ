using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
    }
}
