namespace ProjetoZ.Domain.Entities;

// Linha única (singleton) que guarda quando foi o último fechamento
// semanal do ranking de clipes — permite o job sobreviver a restarts
// sem fechar a semana errada ou duas vezes.
public class ClipeConfig
{
    public int Id { get; set; }

    public DateTime UltimoFechamento { get; set; }

    // Snapshot do vencedor da última semana fechada — os clipes em si são
    // apagados no fechamento, então isso é o que sobra pra exibir em
    // destaque na tela.
    public string? UltimoVencedorTitulo { get; set; }

    public string? UltimoVencedorUrl { get; set; }

    public string? UltimoVencedorAutorNome { get; set; }

    public string? UltimoVencedorAutorAvatar { get; set; }

    public int? UltimoVencedorCurtidas { get; set; }

    public DateTime? UltimoVencedorFechadoEm { get; set; }
}
