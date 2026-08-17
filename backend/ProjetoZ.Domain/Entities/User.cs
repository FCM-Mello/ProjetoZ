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

    // Canal do YouTube vinculado via OAuth do Google — usado pra validar
    // que um clipe postado no ranking semanal realmente pertence ao usuário.
    public string? YoutubeChannelId { get; set; }

    public string? YoutubeChannelNome { get; set; }

    // Banimento é permanente até um admin reverter (não é um timeout
    // automático) — bloqueia login novo (AuthController.SteamCallback) e
    // qualquer endpoint [Authorize] já autenticado (NotBannedAuthorizationHandler).
    public bool Banido { get; set; } = false;

    public string? BanidoMotivo { get; set; }

    public DateTime? BanidoEm { get; set; }
}