using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.Property(n => n.Titulo).HasMaxLength(120);
            builder.Property(n => n.Mensagem).HasMaxLength(2000);
            builder.Property(n => n.Nivel).HasMaxLength(20);

            // GET /minhas filtra por EnviarEm <= agora && ExpiraEm > agora a
            // cada carregamento de página — sem esses dois índices vira
            // table scan conforme a tabela cresce.
            builder.HasIndex(n => n.EnviarEm);
            builder.HasIndex(n => n.ExpiraEm);
        }
    }

    internal class NotificacaoDestinatarioConfiguration : IEntityTypeConfiguration<NotificacaoDestinatario>
    {
        public void Configure(EntityTypeBuilder<NotificacaoDestinatario> builder)
        {
            builder.HasIndex(d => new { d.NotificacaoId, d.UserId }).IsUnique();
            builder.HasIndex(d => d.UserId);
        }
    }

    internal class NotificacaoLeituraConfiguration : IEntityTypeConfiguration<NotificacaoLeitura>
    {
        public void Configure(EntityTypeBuilder<NotificacaoLeitura> builder)
        {
            builder.HasKey(l => new { l.NotificacaoId, l.UserId });
        }
    }
}
