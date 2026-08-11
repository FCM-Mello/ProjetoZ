"use client";

import ProductModal from "../components/ProductModal";
import CategoryModal from "../components/CategoryModal";
import Toolbar from "./components/Toolbar";
import ProdutoGrid from "./components/ProdutoGrid";
import ContextMenu from "./components/ContextMenu";
import { useHome } from "./useHome";
import "./css/home.css";

export default function Home() {
    const {
        isAdmin,

        search,
        setSearch,
        filtroCategoria,
        toggleFiltroCategoria,

        produtosFiltrados,
        categorias,

        showModal,
        setShowModal,
        editingProduct,
        setEditingProduct,
        showCategoryModal,
        setShowCategoryModal,

        deleteMode,
        createMode,
        toggleCreateMode,
        modeDelete,
        modeCreate,

        selectedProducts,
        selectedCategories,
        toggleProduct,
        toggleCategory,

        onExcluirClick,

        iniciarCriacaoProduto,
        iniciarCriacaoCategoria,

        contextMenu,
        abrirMenuContexto,
        editarProduto,

        salvarProduto,
        salvarCategoria,
        comprarProduto,
    } = useHome();

    return (
        <main className="containerHome">
            <Toolbar
                search={search}
                onSearchChange={setSearch}
                categorias={categorias}
                filtroCategoria={filtroCategoria}
                onToggleFiltro={toggleFiltroCategoria}
                selectedCategories={selectedCategories}
                onToggleCategoriaSelecionada={toggleCategory}
                onCriarCategoria={iniciarCriacaoCategoria}
                deleteMode={deleteMode}
                createMode={createMode}
                onCancelar={() => { modeDelete(false); modeCreate(false); }}
                onToggleCreateMode={toggleCreateMode}
                onExcluirClick={onExcluirClick}
                isAdmin={isAdmin}
            />

            <h2 className="section-title">Produtos</h2>

            <ProdutoGrid
                produtos={produtosFiltrados}
                deleteMode={deleteMode}
                createMode={createMode}
                selectedProducts={selectedProducts}
                onToggleSelecionado={toggleProduct}
                onContextMenu={abrirMenuContexto}
                onCriar={iniciarCriacaoProduto}
                onComprar={comprarProduto}
            />

            {showModal && (
                <ProductModal
                    product={editingProduct ?? undefined}
                    categorias={categorias}
                    onClose={() => { setShowModal(false); setEditingProduct(null); }}
                    onSave={salvarProduto}
                />
            )}

            {showCategoryModal && (
                <CategoryModal
                    onClose={() => setShowCategoryModal(false)}
                    onSave={salvarCategoria}
                />
            )}

            {contextMenu && (
                <ContextMenu
                    contextMenu={contextMenu}
                    onEditar={editarProduto}
                />
            )}
        </main>
    );
}
