using Microsoft.EntityFrameworkCore;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public DbSet<Category> Categorys => Set<Category>();

    public DbSet<PedidoCoins> PedidosCoins => Set<PedidoCoins>();

    public DbSet<Compra> Compras => Set<Compra>();

    public DbSet<Sorteio> Sorteios => Set<Sorteio>();

    public DbSet<SorteioParticipante> SorteioParticipantes => Set<SorteioParticipante>();

    public DbSet<Clipe> Clipes => Set<Clipe>();

    public DbSet<ClipeCurtida> ClipeCurtidas => Set<ClipeCurtida>();

    public DbSet<ClipeConfig> ClipeConfigs => Set<ClipeConfig>();

    public DbSet<Seguro> Seguros => Set<Seguro>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<NotificacaoDestinatario> NotificacaoDestinatarios => Set<NotificacaoDestinatario>();

    public DbSet<NotificacaoLeitura> NotificacaoLeituras => Set<NotificacaoLeitura>();

    public DbSet<PlayerRanking> PlayerRankings => Set<PlayerRanking>();
}