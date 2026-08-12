namespace ProjetoZ.Domain.Entities;

public class ClipeCurtida
{
    public Guid Id { get; set; }

    public Guid ClipeId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
