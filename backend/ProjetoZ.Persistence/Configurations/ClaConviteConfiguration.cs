using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ClaConviteConfiguration : IEntityTypeConfiguration<ClaConvite>
    {
        public void Configure(EntityTypeBuilder<ClaConvite> builder)
        {
            // Não dá pra convidar o mesmo jogador duas vezes pro mesmo clã.
            builder.HasIndex(c => new { c.ClaId, c.ConvidadoUserId }).IsUnique();

            builder.HasIndex(c => c.ConvidadoUserId);
        }
    }
}
