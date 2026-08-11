import { Compra } from "../models/Compra";
import { authHeaders } from "./api";

const API_URL = "/api/compras";

export async function getMinhasCompras(): Promise<Compra[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar histórico de compras");

    return response.json();
}
