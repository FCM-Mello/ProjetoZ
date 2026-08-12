import { authHeaders } from "./api";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "/api";

export function loginSteam() {
    window.location.href = `${API_URL}/auth/steam/login`;
}

export function vincularYoutube() {
    const token = localStorage.getItem("token");
    window.location.href = `${API_URL}/auth/youtube/vincular?token=${encodeURIComponent(token ?? "")}`;
}

export async function desvincularYoutube() {
    const response = await fetch(`${API_URL}/auth/youtube/vincular`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao desvincular o canal do YouTube.");
}

export async function getCurrentUser(token: string) {
    const response = await fetch(`${API_URL}/auth/me`, {
        headers: {
            Authorization: `Bearer ${token}`
        }
    });

    if (!response.ok)
        throw new Error("Usuário não autenticado");

    return response.json();
}