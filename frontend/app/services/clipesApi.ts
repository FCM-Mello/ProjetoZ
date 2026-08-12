import { authHeaders } from "./api";
import { ClipesResponse, CreateClipeRequest } from "../models/Clipe";

const API_URL = "/api/clipes";

export async function getClipes(): Promise<ClipesResponse> {
    const response = await fetch(API_URL, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar clipes");

    return response.json();
}

export async function criarClipe(request: CreateClipeRequest) {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(request),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao postar clipe");

    return response.json();
}

export async function curtirClipe(id: string): Promise<{ curtidas: number }> {
    const response = await fetch(`${API_URL}/${id}/curtir`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao curtir");

    return response.json();
}
