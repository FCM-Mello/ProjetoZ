namespace ProjetoZ.Domain.Entities;

public class PedidoCoins
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PackageId { get; set; } = string.Empty;

    public int Coins { get; set; }

    public decimal Valor { get; set; }

    public string Status { get; set; } = "pending";

    public string? MercadoPagoPaymentId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
