"use client";

import { Category } from "../../models/Category";
import CategoriaChips from "./CategoriaChips";
import "../css/toolbar.css";

interface Props {
    search: string;
    onSearchChange: (value: string) => void;

    categorias: Category[];
    filtroCategoria: string;
    onToggleFiltro: (id: string) => void;
    selectedCategories: string[];
    onToggleCategoriaSelecionada: (id: string) => void;
    onCriarCategoria: () => void;

    deleteMode: boolean;
    createMode: boolean;
    onCancelar: () => void;
    onToggleCreateMode: () => void;
    onExcluirClick: () => void;
}

export default function Toolbar({
    search,
    onSearchChange,
    categorias,
    filtroCategoria,
    onToggleFiltro,
    selectedCategories,
    onToggleCategoriaSelecionada,
    onCriarCategoria,
    deleteMode,
    createMode,
    onCancelar,
    onToggleCreateMode,
    onExcluirClick,
}: Props) {
    return (
        <div className="toolbar">
            <div className="filtros">
                <input
                    className="search"
                    type="text"
                    placeholder="Pesquisar..."
                    value={search}
                    onChange={(e) => onSearchChange(e.target.value)}
                />

                <CategoriaChips
                    categorias={categorias}
                    filtroCategoria={filtroCategoria}
                    deleteMode={deleteMode}
                    createMode={createMode}
                    selectedCategories={selectedCategories}
                    onToggleFiltro={onToggleFiltro}
                    onToggleSelecionada={onToggleCategoriaSelecionada}
                    onCriar={onCriarCategoria}
                />
            </div>

            <div className="toolbar-buttons">

                {(deleteMode || createMode) && (
                    <button
                        className="btnCancel"
                        onClick={onCancelar}>
                        Cancelar
                    </button>
                )}

                <button
                    className="btnCreate"
                    onClick={onToggleCreateMode}>
                    Criar
                </button>

                <button
                    className="btnDelete"
                    onClick={onExcluirClick}>
                    Excluir
                </button>

            </div>
        </div>
    );
}
