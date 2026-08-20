using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class ClaConfiguration : IEntityTypeConfiguration<Cla>
    {
        public void Configure(EntityTypeBuilder<Cla> builder)
        {
            // Filtrado pelo mesmo motivo do índice de GrupoModId abaixo — nome
            // de grupo no jogo não é garantido único (o mod nem valida isso),
            // então só clãs criados no site (GrupoModId nulo) entram nessa
            // unicidade. Sem o filtro, dois grupos do mod com nome igual (ou
            // ambos vazios) derrubavam o sync inteiro com 500.
            builder.HasIndex(c => c.Nome)
                .IsUnique()
                .HasFilter("\"GrupoModId\" IS NULL");

            // Chave que o sync do mod usa pra upsert — filtrado porque
            // clãs de origem site (GrupoModId nulo) não entram nessa
            // unicidade (senão só o primeiro clã sem GrupoModId passaria).
            builder.HasIndex(c => c.GrupoModId)
                .IsUnique()
                .HasFilter("\"GrupoModId\" IS NOT NULL");
        }
    }
}
