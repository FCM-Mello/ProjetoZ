namespace ProjetoZ.Domain.Entities;

// Linha única (singleton) que guarda quando foi o último fechamento
// semanal do ranking de clipes — permite o job sobreviver a restarts
// sem fechar a semana errada ou duas vezes.
public class ClipeConfig
{
    public int Id { get; set; }

    public DateTime UltimoFechamento { get; set; }
}
