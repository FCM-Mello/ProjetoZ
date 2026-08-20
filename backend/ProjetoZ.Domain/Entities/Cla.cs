namespace ProjetoZ.Domain.Entities;

// Clã e "Grupo" (conceito que o mod usa) são a mesma coisa — essa entidade
// serve tanto o que é criado pelo site quanto o que o mod gerencia via
// POST /api/game/grupos/adicionar e /expulsar. GrupoModId é nulo pra clãs
// criados no site (o mod não sabe que existem); quando preenchido, é a
// chave que esses dois endpoints usam pra achar o clã certo.
public class Cla
{
    public Guid Id { get; set; }

    public string? GrupoModId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    // data URL base64, igual ao padrão já usado em Product.Imagem. Só existe
    // pro lado do site — o mod não manda estandarte.
    public string? Estandarte { get; set; }

    // Nulo se o líder (identificado por SteamId) ainda não tem conta no
    // site — só pode acontecer pra clãs de origem mod.
    public Guid? LiderUserId { get; set; }

    public string LiderSteamId { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
