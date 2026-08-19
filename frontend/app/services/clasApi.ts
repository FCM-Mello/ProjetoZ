import { ClaResumo, ClaDetalhe, ClaBuscaJogador, CriarClaRequest } from "../models/Cla";
import { authHeaders } from "./api";

const API_URL = "/api/clas";

async function tratarErro(response: Response, mensagemPadrao: string) {
    const mensagem = await response.text();
    throw new Error(mensagem || mensagemPadrao);
}

export async function getClas(): Promise<ClaResumo[]> {
    const response = await fetch(API_URL, { headers: authHeaders() });

    if (!response.ok)
        throw new Error("Erro ao buscar clãs");

    return response.json();
}

export async function getMeuCla(): Promise<ClaDetalhe | null> {
    const response = await fetch(`${API_URL}/meu`, { headers: authHeaders() });

    if (response.status === 204)
        return null;

    if (!response.ok)
        throw new Error("Erro ao buscar seu clã");

    return response.json();
}

export async function getCla(id: string): Promise<ClaDetalhe> {
    const response = await fetch(`${API_URL}/${id}`, { headers: authHeaders() });

    if (!response.ok)
        throw new Error("Erro ao buscar clã");

    return response.json();
}

export async function criarCla(request: CriarClaRequest): Promise<{ id: string }> {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(request),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao criar clã");

    return response.json();
}

export async function solicitarEntrada(claId: string) {
    const response = await fetch(`${API_URL}/${claId}/solicitar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao solicitar entrada");
}

export async function aprovarSolicitacao(claId: string, solicitacaoId: string) {
    const response = await fetch(`${API_URL}/${claId}/solicitacoes/${solicitacaoId}/aprovar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao aprovar solicitação");
}

export async function removerSolicitacao(claId: string, solicitacaoId: string) {
    const response = await fetch(`${API_URL}/${claId}/solicitacoes/${solicitacaoId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao remover solicitação");
}

export async function promoverAdmin(claId: string, userId: string) {
    const response = await fetch(`${API_URL}/${claId}/membros/${userId}/promover`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao promover membro");
}

export async function removerAdmin(claId: string, userId: string) {
    const response = await fetch(`${API_URL}/${claId}/membros/${userId}/remover-admin`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao remover admin");
}

export async function removerMembro(claId: string, userId: string) {
    const response = await fetch(`${API_URL}/${claId}/membros/${userId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao remover membro");
}

export async function sairDoCla(claId: string) {
    const response = await fetch(`${API_URL}/${claId}/sair`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao sair do clã");
}

export async function desfazerCla(claId: string) {
    const response = await fetch(`${API_URL}/${claId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao desfazer clã");
}

export async function buscarJogadorParaConvidar(claId: string, q: string): Promise<ClaBuscaJogador[]> {
    const response = await fetch(`${API_URL}/${claId}/buscar-jogador?q=${encodeURIComponent(q)}`, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar jogador");

    return response.json();
}

export async function convidarParaCla(claId: string, userId: string) {
    const response = await fetch(`${API_URL}/${claId}/convidar/${userId}`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao convidar jogador");
}

export async function aceitarConviteCla(conviteId: string) {
    const response = await fetch(`${API_URL}/convites/${conviteId}/aceitar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao aceitar convite");
}

export async function recusarConviteCla(conviteId: string) {
    const response = await fetch(`${API_URL}/convites/${conviteId}/recusar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        await tratarErro(response, "Erro ao recusar convite");
}
