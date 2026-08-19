using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ClaConfiguration : IEntityTypeConfiguration<Cla>
    {
        public void Configure(EntityTypeBuilder<Cla> builder)
        {
            builder.HasIndex(c => c.Nome).IsUnique();

            // Chave que o sync do mod usa pra upsert — filtrado porque
            // clãs de origem site (GrupoModId nulo) não entram nessa
            // unicidade (senão só o primeiro clã sem GrupoModId passaria).
            builder.HasIndex(c => c.GrupoModId)
                .IsUnique()
                .HasFilter("\"GrupoModId\" IS NOT NULL");
        }
    }
}
