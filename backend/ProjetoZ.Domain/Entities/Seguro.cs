namespace ProjetoZ.Domain.Entities;

// Seguro de um item comprado dentro do jogo (normalmente veículo). Cada item
// segurado vira uma linha própria — o mesmo jogador pode ter vários seguros
// do mesmo ItemId, cada um com seu próprio cooldown de resgate. Dura 1 mês a
// partir da criação (ExpiraEm).
public class Seguro
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    // Id do item no ArkZ_Catalogo.c do mod (ex: "carro") — não é Guid.
    public string ItemId { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    // Nulo enquanto nunca foi resgatado (nesse caso o resgate está liberado).
    public DateTime? UltimoResgate { get; set; }

    // Id do veículo específico no mundo do jogo (ex: net id persistente) —
    // nulo até a primeira sincronização de posição vincular esse seguro a um
    // veículo concreto (ver POST /api/game/veiculos/posicao). O endpoint de
    // criação de seguro (POST /api/game/seguro) não sabe qual veículo é —
    // só o tipo do item — então o vínculo acontece depois, por job do mod.
    public string? CarroId { get; set; }

    public string? VeiculoNome { get; set; }

    public string? PosicaoGrid { get; set; }

    public double? PosicaoX { get; set; }

    public double? PosicaoZ { get; set; }

    public DateTime? PosicaoAtualizadaEm { get; set; }
}
