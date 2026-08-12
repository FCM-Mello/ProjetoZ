export interface AdminUsuario {
    id: string;
    steamId: string;
    nome: string;
    avatar: string;
    coins: number;
    vipNivel: number;
    vipNivelNome: string | null;
    vipExpiraEm: string | null;
    isAdmin: boolean;
}

export interface InventarioItem {
    produtoId: string;
    nome: string;
    quantidade: number;
}

export interface AdminUsuarioDetalhe extends AdminUsuario {
    inventario: InventarioItem[];
}
