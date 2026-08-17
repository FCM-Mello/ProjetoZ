"use client";

import type { MouseEvent } from "react";
import { Product } from "../../models/Product";
import { useScrollReveal } from "../../hooks/useScrollReveal";
import "../css/produtoGrid.css";

interface Props {
    produtos: Product[];
    deleteMode: boolean;
    createMode: boolean;
    selectedProducts: string[];
    onToggleSelecionado: (id: string) => void;
    onContextMenu: (e: MouseEvent, produto: Product) => void;
    onCriar: () => void;
    onComprar: (produto: Product) => void;
}

export default function ProdutoGrid({
    produtos,
    deleteMode,
    createMode,
    selectedProducts,
    onToggleSelecionado,
    onContextMenu,
    onCriar,
    onComprar,
}: Props) {
    const gridRef = useScrollReveal<HTMLDivElement>(produtos.length);

    return (
        <div className="grid-produtos" ref={gridRef}>
            {produtos.map((produto, i) => {

                const selected = selectedProducts.includes(produto.id);

                return (
                    <div
                        key={produto.id}
                        className={`card reveal ${deleteMode && selected ? "selected" : ""}`}
                        style={{ transitionDelay: `${Math.min(i, 8) * 45}ms` }}
                        onClick={() => {
                            if (deleteMode) {
                                onToggleSelecionado(produto.id);
                            }
                        }}
                        onContextMenu={(e) => onContextMenu(e, produto)}
                    >
                        {deleteMode && selected && (
                            <div className="selectedIcon">✓</div>
                        )}

                        <img src={produto.imagem} />

                        <div className="card-body">
                            <h3>{produto.nome}</h3>

                            <p>{produto.descricao}</p>

                            <span>Az coins {produto.preco}</span>

                            {!deleteMode && (
                                <button onClick={(e) => { e.stopPropagation(); onComprar(produto); }}>
                                    Comprar
                                </button>
                            )}
                        </div>
                    </div>
                );
            })}

            {createMode && (
                <div className="card card-add" onClick={onCriar}>
                    <span className="plus-icon">+</span>
                </div>
            )}
        </div>
    );
}
