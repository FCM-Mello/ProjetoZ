"use client";

import type { MouseEvent } from "react";
import { Product } from "../../models/Product";
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
    return (
        <div className="grid-produtos">
            {produtos.map(produto => {

                const selected = selectedProducts.includes(produto.id);

                return (
                    <div
                        key={produto.id}
                        className={`card ${deleteMode && selected ? "selected" : ""}`}
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
