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

            // Só existe pra achar clã antigo de origem mod (grupos/adicionar
            // e /jogador aceitam esse Id além do Guid interno) — clã novo
            // nunca preenche esse campo, então filtrado pra não travar num
            // segundo clã com GrupoModId nulo.
            builder.HasIndex(c => c.GrupoModId)
                .IsUnique()
                .HasFilter("\"GrupoModId\" IS NOT NULL");
        }
    }
}
