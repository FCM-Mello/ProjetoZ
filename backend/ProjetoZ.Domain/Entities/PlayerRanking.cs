namespace ProjetoZ.Domain.Entities;

// Uma linha por usuário — Kills/Deaths são sincronizados como totais
// absolutos pelo mod (não incrementais), enquanto KothCompletados é
// incrementado a cada chamada de conclusão do KOTH.
public class PlayerRanking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int KothCompletados { get; set; }

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
