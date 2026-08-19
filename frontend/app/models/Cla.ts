export interface ClaResumo {
    id: string;
    nome: string;
    descricao: string;
    estandarte: string | null;
    totalMembros: number;
    liderNome: string;
}

export interface ClaMembro {
    // Nulo se o membro veio de sync do mod e nunca fez login no site.
    userId: string | null;
    nome: string;
    avatar: string;
    isLider: boolean;
    isAdmin: boolean;
}

export interface ClaSolicitacao {
    id: string;
    userId: string;
    nome: string;
    avatar: string;
    criadoEm: string;
}

export interface ClaDetalhe {
    id: string;
    nome: string;
    descricao: string;
    estandarte: string | null;
    criadoEm: string;
    membros: ClaMembro[];
    solicitacoes: ClaSolicitacao[];
    souLider: boolean;
    souAdmin: boolean;
    tenhoSolicitacaoPendente: boolean;
}

export interface ClaBuscaJogador {
    userId: string;
    nome: string;
    avatar: string;
}

export interface CriarClaRequest {
    nome: string;
    descricao: string;
    estandarte?: string | null;
}
