namespace ProjetoZ.Domain.Entities;

// Só existe linha aqui quando Notificacao.ParaTodos == false.
public class NotificacaoDestinatario
{
    public Guid Id { get; set; }

    public Guid NotificacaoId { get; set; }

    public Guid UserId { get; set; }
}
