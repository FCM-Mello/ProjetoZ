using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Todo usuário nasce a partir de um login Steam (ver AuthController),
            // então o SteamId é sempre preenchido e nunca se repete.
            builder.OwnsOne(u => u.Profile, profile =>
            {
                profile.HasIndex(p => p.SteamId).IsUnique();
            });

            // Postgres permite múltiplos NULLs num índice único, então usuários
            // sem canal vinculado convivem normalmente com essa restrição — ela
            // só impede que o mesmo canal do YouTube seja vinculado a duas contas.
            builder.HasIndex(u => u.YoutubeChannelId).IsUnique();
        }
    }
}
