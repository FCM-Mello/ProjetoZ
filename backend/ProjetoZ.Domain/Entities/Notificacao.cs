namespace ProjetoZ.Domain.Entities;

public class Notificacao
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    // "verde" | "amarelo" | "vermelho" — ver NotificacaoNiveis.
    public string Nivel { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Guid CriadoPorUserId { get; set; }

    // Quando a notificação passa a aparecer pros destinatários — imediato
    // (CriadoEm) se o admin não agendou, ou uma data futura se agendou.
    public DateTime EnviarEm { get; set; }

    // Sempre EnviarEm + 7 dias, calculado na criação — a notificação some
    // da lista do usuário depois disso (filtro de query, sem job de
    // limpeza, mesmo padrão de Seguro.ExpiraEm).
    public DateTime ExpiraEm { get; set; }

    // Se true, todo usuário vê — não gera linhas em NotificacaoDestinatario.
    public bool ParaTodos { get; set; }

    // "aviso" (padrão, criado pelo admin) | "convite_cla" (criado pelo
    // ClasController ao convidar alguém). O frontend usa isso pra saber se
    // deve mostrar os botões de aceitar/recusar.
    public string Tipo { get; set; } = "aviso";

    // Preenchido só quando Tipo == "convite_cla" — aponta pro convite que o
    // botão aceitar/recusar deve resolver.
    public Guid? ClaConviteId { get; set; }
}
