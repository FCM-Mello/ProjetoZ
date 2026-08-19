namespace ProjetoZ.Domain.Entities;

// Pedido de entrada num clã — some ao ser aprovado (vira ClaMembro) ou
// rejeitado/cancelado (só é removido, sem histórico de status).
public class ClaSolicitacao
{
    public Guid Id { get; set; }

    public Guid ClaId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
