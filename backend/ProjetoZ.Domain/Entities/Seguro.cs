namespace ProjetoZ.Domain.Entities;

// Seguro de um item comprado dentro do jogo (normalmente veículo). Cada item
// segurado vira uma linha própria — o mesmo jogador pode ter vários seguros
// do mesmo ItemId, cada um com seu próprio cooldown de resgate.
public class Seguro
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // Id do item no ArkZ_Catalogo.c do mod (ex: "carro") — não é Guid.
    public string ItemId { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Nulo enquanto nunca foi resgatado (nesse caso o resgate está liberado).
    public DateTime? UltimoResgate { get; set; }
}
