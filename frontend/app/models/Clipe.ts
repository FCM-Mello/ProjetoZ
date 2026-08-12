export interface Clipe {
    id: string;
    titulo: string;
    url: string;
    autorNome: string;
    autorAvatar: string;
    autorSouEu: boolean;
    curtidas: number;
    jaCurti: boolean;
    criadoEm: string;
}

export interface ClipesResponse {
    proximoFechamento: string;
    clipes: Clipe[];
}

export interface CreateClipeRequest {
    titulo: string;
    url: string;
}
