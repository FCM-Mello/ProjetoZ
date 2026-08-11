import { CreateSorteioRequest, Sorteio } from "../models/Sorteio";
import { authHeaders } from "./api";

const API_URL = "/api/sorteios";

export async function getSorteios(): Promise<Sorteio[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar sorteios");

    return response.json();
}

export async function criarSorteio(request: CreateSorteioRequest) {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        const mensagem = await response.text();
        throw new Error(mensagem || "Erro ao criar sorteio");
    }
}

export async function entrarSorteio(id: string) {
    const response = await fetch(`${API_URL}/${id}/entrar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok) {
        const mensagem = await response.text();
        throw new Error(mensagem || "Erro ao entrar no sorteio");
    }
}

export async function sortearSorteio(id: string): Promise<{ vencedorUserId: string; vencedorNome: string | null }> {
    const response = await fetch(`${API_URL}/${id}/sortear`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok) {
        const mensagem = await response.text();
        throw new Error(mensagem || "Erro ao sortear");
    }

    return response.json();
}
