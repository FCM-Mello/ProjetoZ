import { authHeaders } from "./api";
import { RankingJogador } from "../models/Ranking";

const API_URL = "/api/ranking";

export async function getRanking(): Promise<RankingJogador[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar ranking");

    return response.json();
}

export async function resetarRanking() {
    const response = await fetch(API_URL, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao resetar ranking");
}
