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

        modelBuilder.Entity<User>()
            .OwnsOne(x => x.Profile);
    }

    public DbSet<Category> Categorys => Set<Category>();

    public DbSet<PedidoCoins> PedidosCoins => Set<PedidoCoins>();
}