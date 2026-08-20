namespace ProjetoZ.Domain.Entities;

// Clã e "Grupo" (conceito que o mod usa) são a mesma coisa. Clã só é
// criado pelo site (ClasController.Criar) — o mod só reflete
// entrada/saída de membro num clã que já existe, via
// POST /api/game/grupos/adicionar e /expulsar. GrupoModId existe só por
// compatibilidade com clãs antigos de quando o mod ainda podia criar
// grupo direto (sync em lote, removido); código novo nunca preenche.
public class Cla
{
    public Guid Id { get; set; }

    public string? GrupoModId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    // data URL base64, igual ao padrão já usado em Product.Imagem. Só existe
    // pro lado do site — o mod não manda estandarte.
    public string? Estandarte { get; set; }

    // Sempre preenchido pra clã novo (criador já é usuário logado do
    // site). Só fica nulo em clã antigo de origem mod, de antes do líder
    // ter feito login com essa SteamId.
    public Guid? LiderUserId { get; set; }

    public string LiderSteamId { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
