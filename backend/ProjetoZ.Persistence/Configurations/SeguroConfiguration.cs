using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class SeguroConfiguration : IEntityTypeConfiguration<Seguro>
    {
        public void Configure(EntityTypeBuilder<Seguro> builder)
        {
            // O mod lista os seguros de um jogador a cada consulta, sempre
            // filtrando por UserId.
            builder.HasIndex(s => s.UserId);

            builder.Property(s => s.ItemId).HasMaxLength(100);

            // Postgres permite múltiplos NULLs num índice único — só passa a
            // impedir duplicidade depois que um CarroId é de fato atribuído
            // pela sincronização de posição.
            builder.HasIndex(s => s.CarroId).IsUnique();
        }
    }
}
