using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ClaMembroConfiguration : IEntityTypeConfiguration<ClaMembro>
    {
        public void Configure(EntityTypeBuilder<ClaMembro> builder)
        {
            // Um jogador (por SteamId) só pode estar em um clã por vez.
            builder.HasIndex(m => m.SteamId).IsUnique();

            builder.HasIndex(m => m.ClaId);
        }
    }
}
