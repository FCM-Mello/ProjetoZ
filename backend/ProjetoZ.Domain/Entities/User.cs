using ProjetoZ.Domian.Models;

namespace ProjetoZ.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public SteamProfile? Profile { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime UltimoLogin { get; set; }

    public int Coins { get; set; } = 0;

    public List<Guid> Inventario { get; set; } = new List<Guid>();

    // VIP é um atributo fixo do usuário (não um produto): nível 0 = sem VIP,
    // 1-3 = Bronze/Prata/Ouro. Expira automaticamente após VipExpiraEm.
    public int VipNivel { get; set; } = 0;

    public DateTime? VipExpiraEm { get; set; }

    public bool IsAdmin { get; set; } = false;
}