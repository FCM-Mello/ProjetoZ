export type NivelNotificacao = "verde" | "amarelo" | "vermelho";

export interface Notificacao {
    id: string;
    titulo: string;
    mensagem: string;
    nivel: NivelNotificacao;
    enviarEm: string;
    lida: boolean;
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
