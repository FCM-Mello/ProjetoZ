import { Product } from "../models/Product";
import { authHeaders } from "./api"

const API_URL = "/api/products";

export async function getProducts(): Promise<Product[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders()
    });

    if (!response.ok)
        throw new Error("Erro ao buscar produtos");

    return response.json();
}

export async function createProduct(product: Product) {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(product),
    });

    if (!response.ok)
        throw new Error("Erro ao cadastrar produto");
}

export async function deleteProduct(id: number) {
    const response = await fetch(`${API_URL}/${id}`, {
        headers: authHeaders(),
        method: "DELETE",
    });

    if (!response.ok)
        throw new Error("Erro ao excluir produto");
}