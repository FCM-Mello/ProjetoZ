using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoZ.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;
    }
}
