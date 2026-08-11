export interface Compra {
    id: string;
    tipo: "produto" | "coins";
    descricao: string;
    coins: number;
    valorReais: number | null;
    criadoEm: string;
}
