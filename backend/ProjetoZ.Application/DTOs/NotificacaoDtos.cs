using System.ComponentModel.DataAnnotations;

namespace ProjetoZ.Application.DTOs
{
    public class CriarNotificacaoRequest
    {
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mensagem é obrigatória.")]
        [StringLength(2000)]
        public string Mensagem { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nível é obrigatório.")]
        public string Nivel { get; set; } = string.Empty;

        public bool ParaTodos { get; set; }

        // Obrigatório (e não vazio) quando ParaTodos == false.
        public List<Guid>? DestinatarioUserIds { get; set; }

        // Nulo = envia imediatamente. No futuro = agendada, só aparece pros
        // destinatários a partir dessa data/hora.
        public DateTime? EnviarEm { get; set; }
    }

    // Visão do usuário logado (GET /api/notificacoes/minhas).
    public class NotificacaoDto
    {
        public Guid Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Mensagem { get; set; } = string.Empty;

        public string Nivel { get; set; } = string.Empty;

        public DateTime EnviarEm { get; set; }

        public bool Lida { get; set; }

        // "aviso" | "convite_cla" — o frontend só mostra os botões de
        // aceitar/recusar quando é convite_cla.
        public string Tipo { get; set; } = "aviso";

        public Guid? ClaConviteId { get; set; }
    }

    // Visão do admin (GET /api/notificacoes, histórico completo).
    public class NotificacaoAdminDto
    {
        public Guid Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Mensagem { get; set; } = string.Empty;

        public string Nivel { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }

        public DateTime EnviarEm { get; set; }

        public DateTime ExpiraEm { get; set; }

        public bool ParaTodos { get; set; }

        public int TotalDestinatarios { get; set; }

        public int TotalLeituras { get; set; }
    }
}
