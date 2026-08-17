namespace ProjetoZ.Domain.Entities;

// Uma linha por (Notificacao, User) que já marcou como lida — vale tanto
// pra notificação ParaTodos quanto direcionada.
public class NotificacaoLeitura
{
    public Guid NotificacaoId { get; set; }

    public Guid UserId { get; set; }

    public DateTime LidaEm { get; set; } = DateTime.UtcNow;
}
