namespace ProjetoZ.Domain.Entities;

public class Compra
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Tipo { get; set; } = string.Empty; // "produto" ou "coins"

    public string Descricao { get; set; } = string.Empty;

    public int Coins { get; set; }

    public decimal? ValorReais { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
