using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class PurchaseCoinsRequest
    {
        [Required(ErrorMessage = "Pacote inválido.")]
        public string PackageId { get; set; } = string.Empty;
    }
}
