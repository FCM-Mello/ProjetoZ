"use client";

import { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { getCategories } from "../services/categoriesApi";
import { Category } from "../models/Category";
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
    const [categorias, setCategorias] = useState<Category[]>([]);

    useEffect(() => {
        getCategories().then(setCategorias).catch(console.error);
    }, []);

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

    const categoriaVip = categorias.find(c => c.nome.toLowerCase() === "vip");
    const vipAtivo = categoriaVip ? itens.some(p => p.categoria === categoriaVip.id) : false;

    const totalSlots = Math.max(
        COLUNAS * LINHAS_MINIMAS,
        Math.ceil((itensAgrupados.length + 1) / COLUNAS) * COLUNAS
    );

    const slotsVazios = Math.max(0, totalSlots - itensAgrupados.length);

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
                            <span className="vipTitulo">VIP Ativo</span>
                            <span className="vipDescricao">Você tem acesso aos benefícios exclusivos de membro VIP.</span>
                        </div>
                    </>
                ) : (
                    <>
                        <div className="vipTexto">
                            <span className="vipTitulo">Você ainda não é VIP</span>
                            <span className="vipDescricao">Adquira um item VIP na loja para desbloquear benefícios exclusivos.</span>
                        </div>

                        <a
                            className="vipButton"
                            href={categoriaVip ? `/Home?categoria=${categoriaVip.id}` : "/Home"}
                        >
                            Ver itens VIP na loja
                        </a>
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
