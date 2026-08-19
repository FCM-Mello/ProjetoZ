namespace ProjetoZ.Domain.Entities;

// Um jogador (por SteamId) só pode estar num clã por vez — SteamId é a
// identidade "de verdade" aqui porque um membro sincronizado do mod pode
// nunca ter feito login no site (UserId nulo nesse caso). O líder também
// tem uma linha aqui (com IsAdmin=true) — Cla.LiderSteamId é quem decide
// "é o líder", IsAdmin decide "pode aprovar/gerenciar" pelo site.
public class ClaMembro
{
    public Guid Id { get; set; }

    public Guid ClaId { get; set; }

    public Guid? UserId { get; set; }

    public string SteamId { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public DateTime EntrouEm { get; set; } = DateTime.UtcNow;
}
