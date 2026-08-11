"use client";

import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { Product } from "../models/Product";
import "./page.css";

const COLUNAS = 8;
const LINHAS_MINIMAS = 3;

interface ItemAgrupado {
    produto: Product;
    quantidade: number;
}

export default function Inventario() {
    useRequireAuth();

    const { user } = useAuth();

    const itens = user?.inventario ?? [];

    const itensAgrupados = Object.values(
        itens.reduce<Record<string, ItemAgrupado>>((acc, produto) => {
            if (!acc[produto.id]) {
                acc[produto.id] = { produto, quantidade: 0 };
            }

            acc[produto.id].quantidade += 1;

            return acc;
        }, {})
    );

    const vipAtivo = (user?.vipNivel ?? 0) > 0;

    const totalSlots = Math.max(
        COLUNAS * LINHAS_MINIMAS,
        Math.ceil((itensAgrupados.length + 1) / COLUNAS) * COLUNAS
    );

    const slotsVazios = Math.max(0, totalSlots - itensAgrupados.length);

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR");
    }

    return (
        <main className="containerInventario">
            <div className="inventarioHeader">
                <h2 className="section-title">Inventário</h2>

                <span className="inventarioCount">{itens.length} {itens.length === 1 ? "item" : "itens"}</span>
            </div>

            <div className={`vipSection ${vipAtivo ? "vip-ativo" : "vip-inativo"}`}>
                {vipAtivo ? (
                    <>
                        <span className="vipIcone">★</span>

                        <div className="vipTexto">
                            <span className="vipTitulo">VIP {user!.vipNivelNome} Ativo</span>
                            <span className="vipDescricao">
                                {user!.vipExpiraEm && `Válido até ${formatarData(user!.vipExpiraEm)}.`} Você tem acesso aos benefícios exclusivos de membro VIP.
                            </span>
                        </div>

                        <Link className="vipButton" href="/Vip">
                            Renovar / upgrade
                        </Link>
                    </>
                ) : (
                    <>
                        <div className="vipTexto">
                            <span className="vipTitulo">Você ainda não é VIP</span>
                            <span className="vipDescricao">Compre um dos planos VIP e desbloqueie benefícios exclusivos no servidor.</span>
                        </div>

                        <Link className="vipButton" href="/Vip">
                            Ver planos VIP
                        </Link>
                    </>
                )}
            </div>

            <div className="maleta">
                <div className="maleta-grid">
                    {itensAgrupados.map(({ produto, quantidade }) => (
                        <div key={produto.id} className="slot">
                            <div className="slot-imagem">
                                <img src={produto.imagem} alt={produto.nome} />
                            </div>

                            {quantidade > 1 && (
                                <span className="slot-quantidade">{quantidade}</span>
                            )}

                            <div className="slot-info">
                                <span className="slot-nome">{produto.nome}</span>
                                <span className="slot-preco">🪙 {produto.preco}</span>
                            </div>
                        </div>
                    ))}

                    {Array.from({ length: slotsVazios }).map((_, i) => (
                        <div key={`vazio-${i}`} className="slot slot-vazio" />
                    ))}
                </div>
            </div>
        </main>
    );
}
