import { VipTier } from "../models/VipTier";
import { authHeaders } from "./api";

const API_URL = "/api/vip";

export async function getVipTiers(): Promise<VipTier[]> {
    const response = await fetch(`${API_URL}/niveis`);

    if (!response.ok)
        throw new Error("Erro ao buscar níveis de VIP");

    return response.json();
}

export async function comprarVip(nivel: number): Promise<{ coins: number; vipNivel: number; vipNivelNome: string; vipExpiraEm: string }> {
    const response = await fetch(`${API_URL}/comprar/${nivel}`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok) {
        const mensagem = await response.text();
        throw new Error(mensagem || "Erro ao comprar VIP");
    }

    return response.json();
}
