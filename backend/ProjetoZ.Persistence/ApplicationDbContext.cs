using Microsoft.EntityFrameworkCore;
using ProjetoZ.Domain.Entities;

namespace ProjetoZ.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}