import { Category } from "../models/Category";
import { authHeaders } from "./api";

const API_URL = "/api/category";

export async function getCategories(): Promise<Category[]> {
    const response = await fetch(API_URL, {
        headers: authHeaders()
    });

    if (!response.ok)
        throw new Error("Erro ao buscar categorias");

    return response.json();
}

export async function createCategory(category: Category) {
    const response = await fetch(API_URL, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(category),
    });

    if (!response.ok)
        throw new Error("Erro ao cadastrar categoria");

    return response.json();
}

export async function deleteCategory(id: string) {
    const response = await fetch(`${API_URL}/${id}`, {
        headers: authHeaders(),
        method: "DELETE",
    });

    if (!response.ok)
        throw new Error("Erro ao excluir categoria");
}
