import { authHeaders } from "./api";

const API_URL = "/api/seguros";

export interface SeguroAtivo {
    idSeguro: string;
    id: string;
    expiraEm: string;
    carroId: string | null;
    veiculoNome: string | null;
    posicaoGrid: string | null;
    posicaoX: number | null;
    posicaoZ: number | null;
    posicaoAtualizadaEm: string | null;
}

export async function getMeusSeguros(): Promise<SeguroAtivo[]> {
    const response = await fetch(`${API_URL}/meus`, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar seguros");

    return response.json();
}
