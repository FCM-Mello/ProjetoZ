"use client";

import { Category } from "../../models/Category";
import "../css/categoriaChips.css";

interface Props {
    categorias: Category[];
    filtroCategoria: string;
    deleteMode: boolean;
    createMode: boolean;
    selectedCategories: string[];
    onToggleFiltro: (id: string) => void;
    onToggleSelecionada: (id: string) => void;
    onCriar: () => void;
}

export default function CategoriaChips({
    categorias,
    filtroCategoria,
    deleteMode,
    createMode,
    selectedCategories,
    onToggleFiltro,
    onToggleSelecionada,
    onCriar,
}: Props) {
    return (
        <div className="categorias-filtro">
            {categorias.map(categoria => {

                const selecionadaParaExcluir = selectedCategories.includes(categoria.id);
                const ativaComoFiltro = !deleteMode && filtroCategoria === categoria.id;

                return (
                    <div
                        key={categoria.id}
                        className={`category-chip ${ativaComoFiltro ? "active" : ""} ${deleteMode && selecionadaParaExcluir ? "selected" : ""}`}
                        onClick={() => {
                            if (deleteMode) {
                                onToggleSelecionada(categoria.id);
                            } else {
                                onToggleFiltro(categoria.id);
                            }
                        }}
                    >
                        {deleteMode && selecionadaParaExcluir && (
                            <span className="chip-check">✓</span>
                        )}

                        {categoria.nome}
                    </div>
                );
            })}

            {createMode && (
                <div className="category-chip category-chip-add" onClick={onCriar}>
                    <span className="plus-icon">+</span>
                </div>
            )}
        </div>
    );
}
