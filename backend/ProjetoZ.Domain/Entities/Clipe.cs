namespace ProjetoZ.Domain.Entities;

public class Clipe
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
