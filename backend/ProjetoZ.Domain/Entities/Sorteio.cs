namespace ProjetoZ.Domain.Entities;

public class Sorteio
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    // Prêmio: nível de VIP (opcional) e/ou produtos (opcional) — pelo menos um dos dois.
    public int? PremioVipNivel { get; set; }

    public List<Guid> PremioProdutoIds { get; set; } = new();

    public string Status { get; set; } = "aberto"; // aberto | sorteado

    public Guid? VencedorUserId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? SorteadoEm { get; set; }
}
