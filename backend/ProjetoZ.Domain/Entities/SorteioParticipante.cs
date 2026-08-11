namespace ProjetoZ.Domain.Entities;

public class SorteioParticipante
{
    public Guid Id { get; set; }

    public Guid SorteioId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
