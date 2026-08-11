import { VipTier } from "../models/VipTier";

const API_URL = "/api/vip";

export async function getVipTiers(): Promise<VipTier[]> {
    const response = await fetch(`${API_URL}/niveis`);

    if (!response.ok)
        throw new Error("Erro ao buscar níveis de VIP");

    return response.json();
}
