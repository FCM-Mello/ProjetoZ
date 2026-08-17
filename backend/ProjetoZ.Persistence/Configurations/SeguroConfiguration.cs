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

            // Não é único no banco — um CarroId único "pra sempre" impediria
            // segurar de novo o mesmo veículo depois que o seguro anterior
            // expira (a linha antiga continua existindo, só marcada como
            // expirada). A unicidade que importa é só "entre seguros ainda
            // ativos", que é checada na aplicação (GameController), não dá
            // pra expressar isso num índice do Postgres porque a condição
            // depende da hora atual.
            builder.HasIndex(s => s.CarroId);
        }
    }
}
