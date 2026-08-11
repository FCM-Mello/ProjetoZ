export interface SorteioProduto {
    id: string;
    nome: string;
    imagem: string;
}

export interface Sorteio {
    id: string;
    titulo: string;
    descricao: string;
    premioVipNivel: number | null;
    premioVipNivelNome: string | null;
    premioProdutos: SorteioProduto[];
    status: "aberto" | "sorteado";
    totalParticipantes: number;
    jaParticipando: boolean;
    vencedorNome: string | null;
    criadoEm: string;
    sorteadoEm: string | null;
}

export interface CreateSorteioRequest {
    titulo: string;
    descricao: string;
    premioVipNivel: number | null;
    premioProdutoIds: string[];
}
