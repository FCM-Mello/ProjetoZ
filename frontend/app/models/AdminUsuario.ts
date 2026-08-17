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
    banido: boolean;
    banidoMotivo: string | null;
}

export interface InventarioItem {
    produtoId: string;
    nome: string;
    quantidade: number;
}

export interface AdminSeguro {
    idSeguro: string;
    id: string;
    expiraEm: string;
    carroId: string | null;
    veiculoNome: string | null;
}

export interface AdminCompra {
    tipo: string;
    descricao: string;
    coins: number;
    valorReais: number | null;
    criadoEm: string;
}

export interface AdminUsuarioDetalhe extends AdminUsuario {
    inventario: InventarioItem[];
    seguros: AdminSeguro[];
    compras: AdminCompra[];
}
