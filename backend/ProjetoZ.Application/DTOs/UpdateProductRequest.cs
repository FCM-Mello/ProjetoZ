using ProjetoZ.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class UpdateProductRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [Range(0.01, 1_000_000, ErrorMessage = "Preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        [Required(ErrorMessage = "Imagem é obrigatória.")]
        public string Imagem { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Descricao { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Estoque não pode ser negativo.")]
        public int Estoque { get; set; }

        public Guid Categoria { get; set; }
    }
}
