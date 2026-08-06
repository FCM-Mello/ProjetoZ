using ProjetoZ.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoZ.Application.DTOs
{
    public class CreateProductRequest
    {
        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public string Imagem { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int Estoque { get; set; }

        public Guid Categoria { get; set; }
    }
}
