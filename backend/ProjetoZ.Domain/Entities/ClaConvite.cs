namespace ProjetoZ.Domain.Entities;

// Convite de entrada enviado por líder/admin pra um jogador específico —
// diferente de ClaSolicitacao (que é o jogador pedindo pra entrar). Some ao
// ser aceito (vira ClaMembro) ou recusado, sem histórico de status. A
// notificação que avisa o jogador (Notificacao.ClaConviteId) é criada e
// removida junto com esse registro.
public class ClaConvite
{
    public Guid Id { get; set; }

    public Guid ClaId { get; set; }

    public Guid ConvidadoUserId { get; set; }

    public Guid ConvidadoPorUserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
