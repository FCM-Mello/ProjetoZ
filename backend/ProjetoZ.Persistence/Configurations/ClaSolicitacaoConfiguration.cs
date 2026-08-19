using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ClaSolicitacaoConfiguration : IEntityTypeConfiguration<ClaSolicitacao>
    {
        public void Configure(EntityTypeBuilder<ClaSolicitacao> builder)
        {
            // Não dá pra pedir entrada duas vezes no mesmo clã.
            builder.HasIndex(s => new { s.ClaId, s.UserId }).IsUnique();

            builder.HasIndex(s => s.UserId);
        }
    }
}
