import { authHeaders } from "./api";
import { AdminUsuario, AdminUsuarioDetalhe } from "../models/AdminUsuario";
import { AdminCla, AdminClaDetalhe } from "../models/AdminCla";

const API_URL = "/api/admin";

export async function getUsuarios(busca?: string): Promise<AdminUsuario[]> {
    const query = busca ? `?busca=${encodeURIComponent(busca)}` : "";

    const response = await fetch(`${API_URL}/usuarios${query}`, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar usuários");

    return response.json();
}

export async function getUsuario(id: string): Promise<AdminUsuarioDetalhe> {
    const response = await fetch(`${API_URL}/usuarios/${id}`, {
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error("Erro ao buscar usuário");

    return response.json();
}

export async function ajustarCoins(id: string, delta: number): Promise<{ coins: number }> {
    const response = await fetch(`${API_URL}/usuarios/${id}/coins`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ delta }),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao ajustar coins");

    return response.json();
}

export async function zerarCoins(id: string): Promise<{ coins: number }> {
    const response = await fetch(`${API_URL}/usuarios/${id}/coins/zerar`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao zerar coins");

    return response.json();
}

export async function definirVip(id: string, nivel: number) {
    const response = await fetch(`${API_URL}/usuarios/${id}/vip`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ nivel }),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao definir VIP");

    return response.json();
}

export async function removerVip(id: string) {
    const response = await fetch(`${API_URL}/usuarios/${id}/vip`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao remover VIP");
}

export async function adicionarProduto(id: string, produtoId: string): Promise<AdminUsuarioDetalhe> {
    const response = await fetch(`${API_URL}/usuarios/${id}/inventario`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ produtoId }),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao adicionar produto");

    return response.json();
}

export async function removerProduto(id: string, produtoId: string): Promise<AdminUsuarioDetalhe> {
    const response = await fetch(`${API_URL}/usuarios/${id}/inventario/${produtoId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao remover produto");

    return response.json();
}

export async function tornarAdmin(id: string): Promise<AdminUsuario> {
    const response = await fetch(`${API_URL}/usuarios/${id}/admin`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao tornar admin");

    return response.json();
}

export async function removerAdmin(id: string): Promise<AdminUsuario> {
    const response = await fetch(`${API_URL}/usuarios/${id}/admin`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao remover admin");

    return response.json();
}

export async function banirUsuario(id: string, motivo?: string): Promise<AdminUsuario> {
    const response = await fetch(`${API_URL}/usuarios/${id}/banir`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ motivo }),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao banir usuário");

    return response.json();
}

export async function removerBan(id: string): Promise<AdminUsuario> {
    const response = await fetch(`${API_URL}/usuarios/${id}/banir`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao remover banimento");

    return response.json();
}

export async function getClasAdmin(): Promise<AdminCla[]> {
    const response = await fetch(`${API_URL}/clas`, { headers: authHeaders() });

    if (!response.ok)
        throw new Error("Erro ao buscar clãs");

    return response.json();
}

export async function getClaAdmin(id: string): Promise<AdminClaDetalhe> {
    const response = await fetch(`${API_URL}/clas/${id}`, { headers: authHeaders() });

    if (!response.ok)
        throw new Error("Erro ao buscar clã");

    return response.json();
}

export async function removerMembroClaAdmin(claId: string, userId: string) {
    const response = await fetch(`${API_URL}/clas/${claId}/membros/${userId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao remover membro");
}

export async function desfazerClaAdmin(claId: string) {
    const response = await fetch(`${API_URL}/clas/${claId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });

    if (!response.ok)
        throw new Error(await response.text() || "Erro ao desfazer clã");
}
