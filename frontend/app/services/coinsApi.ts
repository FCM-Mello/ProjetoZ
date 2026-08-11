import { CoinPackage } from "../models/CoinPackage";
import { authHeaders } from "./api";

const API_URL = "/api/coins";

export async function getCoinPackages(): Promise<CoinPackage[]> {
    const response = await fetch(`${API_URL}/pacotes`);

    if (!response.ok)
        throw new Error("Erro ao buscar pacotes de coins");

    return response.json();
}

export async function criarCheckoutCoins(packageId: string): Promise<{ redirectUrl: string }> {
    const response = await fetch(`${API_URL}/checkout`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ packageId }),
    });

    if (!response.ok) {
        const mensagem = await response.text();
        throw new Error(mensagem || "Erro ao iniciar o pagamento");
    }

    return response.json();
}
