using ProjetoZ.Domian.Models;

namespace ProjetoZ.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public SteamProfile? Profile { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime UltimoLogin { get; set; }

    public int Coins { get; set; } = 0;
    
    public List<Product> Inventario { get; set; } = new List<Product>();
}