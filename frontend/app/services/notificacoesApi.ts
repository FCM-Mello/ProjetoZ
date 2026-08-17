import { authHeaders } from "./api";
import { Notificacao, NotificacaoAdmin, NivelNotificacao } from "../models/Notificacao";

const API_URL = "/api/notificacoes";

export async function getMinhasNotificacoes(): Promise<Notificacao[]> {
    const response = await fetch(`${API_URL}/minhas`, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar notificações");

    return response.json();
}

export async function marcarLida(id: string) {
    const response = await fetch(`${API_URL}/${id}/lida`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao marcar notificação como lida");
}

export async function marcarTodasLidas() {
    const response = await fetch(`${API_URL}/lidas`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao marcar notificações como lidas");
}

export interface CriarNotificacaoRequest {
    titulo: string;
    mensagem: string;
    nivel: NivelNotificacao;
    paraTodos: boolean;
    destinatarioUserIds?: string[];
    enviarEm?: string;
}

export async function criarNotificacao(request: CriarNotificacaoRequest) {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(request),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao criar notificação");

    return response.json();
}

export async function getTodasNotificacoes(): Promise<NotificacaoAdmin[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar notificações");

    return response.json();
}

export async function excluirNotificacao(id: string) {
    const response = await fetch(`${API_URL}/${id}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao excluir notificação");
}
