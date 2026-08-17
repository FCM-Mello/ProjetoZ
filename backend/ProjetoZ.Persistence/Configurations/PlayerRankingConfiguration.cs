using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class PlayerRankingConfiguration : IEntityTypeConfiguration<PlayerRanking>
    {
        public void Configure(EntityTypeBuilder<PlayerRanking> builder)
        {
            // Uma linha de ranking por usuário — os endpoints do mod fazem
            // upsert (busca por UserId, cria se não existir).
            builder.HasIndex(r => r.UserId).IsUnique();
        }
    }
}
