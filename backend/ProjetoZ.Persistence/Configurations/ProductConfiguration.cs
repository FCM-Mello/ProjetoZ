using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Acelera a checagem de "existem produtos usando essa categoria?"
            // feita antes de excluir uma Category (CategoryController.Delete).
            builder.HasIndex(p => p.Categoria);
        }
    }
}
