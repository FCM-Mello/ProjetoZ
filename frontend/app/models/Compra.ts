export interface Compra {
    id: string;
    tipo: "produto" | "coins" | "sorteio";
    descricao: string;
    coins: number;
    valorReais: number | null;
    criadoEm: string;
}
