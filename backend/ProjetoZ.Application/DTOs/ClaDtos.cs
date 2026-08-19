using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class CriarClaRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Nome precisa ter entre 3 e 40 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Descrição muito longa (máximo 300 caracteres).")]
        public string Descricao { get; set; } = string.Empty;

        public string? Estandarte { get; set; }
    }

    // Card da lista de clãs.
    public class ClaResumoDto
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string? Estandarte { get; set; }

        public int TotalMembros { get; set; }

        public string LiderNome { get; set; } = string.Empty;
    }

    public class ClaMembroDto
    {
        // Nulo se o membro veio de sync do mod e nunca fez login no site —
        // ações de gestão (promover/remover admin) não têm como mirar
        // esse membro nesse caso.
        public Guid? UserId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public bool IsLider { get; set; }

        public bool IsAdmin { get; set; }
    }

    public class ClaSolicitacaoDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
    }

    // Detalhe completo — GET /api/clas/{id} e GET /api/clas/meu.
    // Solicitacoes só vem preenchido se quem pediu for líder/admin do clã.
    public class ClaDetalheDto
    {
        public Guid Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string? Estandarte { get; set; }

        public DateTime CriadoEm { get; set; }

        public List<ClaMembroDto> Membros { get; set; } = new();

        public List<ClaSolicitacaoDto> Solicitacoes { get; set; } = new();

        public bool SouLider { get; set; }

        public bool SouAdmin { get; set; }

        public bool TenhoSolicitacaoPendente { get; set; }
    }
}
