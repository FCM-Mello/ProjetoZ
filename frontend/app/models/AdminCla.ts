export interface AdminCla {
    id: string;
    nome: string;
    descricao: string;
    estandarte: string | null;
    grupoModId: string | null;
    liderNome: string;
    totalMembros: number;
    criadoEm: string;
}

export interface AdminClaMembro {
    userId: string | null;
    steamId: string;
    nome: string;
    avatar: string;
    isLider: boolean;
    isAdmin: boolean;
}

export interface AdminClaDetalhe extends AdminCla {
    membros: AdminClaMembro[];
}
