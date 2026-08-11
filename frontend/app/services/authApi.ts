const API_URL = process.env.NEXT_PUBLIC_API_URL || "/api";

export function loginSteam() {
    window.location.href = `${API_URL}/auth/steam/login`;
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