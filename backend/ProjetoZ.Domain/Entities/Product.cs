using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoZ.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public string Imagem { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int Estoque { get; set; }

        public Guid Categoria { get; set; } = new Guid();
    }
}
