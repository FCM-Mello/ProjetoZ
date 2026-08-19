export type NivelNotificacao = "verde" | "amarelo" | "vermelho";

export type TipoNotificacao = "aviso" | "convite_cla";

export interface Notificacao {
    id: string;
    titulo: string;
    mensagem: string;
    nivel: NivelNotificacao;
    enviarEm: string;
    lida: boolean;
    tipo: TipoNotificacao;
    claConviteId: string | null;
}

export interface NotificacaoAdmin {
    id: string;
    titulo: string;
    mensagem: string;
    nivel: NivelNotificacao;
    criadoEm: string;
    enviarEm: string;
    expiraEm: string;
    paraTodos: boolean;
    totalDestinatarios: number;
    totalLeituras: number;
}
