export interface Compra {
    id: string;
    tipo: "produto" | "coins" | "sorteio" | "vip" | "mod";
    descricao: string;
    coins: number;
    valorReais: number | null;
    criadoEm: string;
}
