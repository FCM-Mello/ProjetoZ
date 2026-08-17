"use client";

import { useEffect, useState, type MouseEvent } from "react";
import { getProducts, createProduct, updateProduct, deleteProduct, purchaseProduct } from "../services/productsApi";
import { getCategories, createCategory, deleteCategory } from "../services/categoriesApi";
import { Product } from "../models/Product";
import { Category } from "../models/Category";
import { useAuth } from "../contexts/AuthContext";
import { useToast } from "../contexts/ToastContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { ContextMenuState } from "./types";

export function useHome() {
    const [search, setSearch] = useState("");
    const [filtroCategoria, setFiltroCategoria] = useState(() => {
        if (typeof window === "undefined") return "";
        return new URLSearchParams(window.location.search).get("categoria") ?? "";
    });

    const [produtos, setProdutos] = useState<Product[]>([]);
    const [categorias, setCategorias] = useState<Category[]>([]);

    const [showModal, setShowModal] = useState(false);
    const [editingProduct, setEditingProduct] = useState<Product | null>(null);
    const [showCategoryModal, setShowCategoryModal] = useState(false);

    const [deleteMode, setDeleteMode] = useState(false);
    const [createMode, setCreateMode] = useState(false);

    const [selectedProducts, setSelectedProducts] = useState<string[]>([]);
    const [selectedCategories, setSelectedCategories] = useState<string[]>([]);

    const [contextMenu, setContextMenu] = useState<ContextMenuState | null>(null);

    const { user, refreshUser } = useAuth();
    const { sucesso, erro: mostrarErro } = useToast();
    const isAdmin = user?.isAdmin ?? false;

    useRequireAuth();

    useEffect(() => {
        carregarProdutos();
        carregarCategorias();
    }, []);

    useEffect(() => {
        if (!contextMenu) return;

        function fecharMenu() {
            setContextMenu(null);
        }

        window.addEventListener("click", fecharMenu);
        window.addEventListener("scroll", fecharMenu, true);

        return () => {
            window.removeEventListener("click", fecharMenu);
            window.removeEventListener("scroll", fecharMenu, true);
        };
    }, [contextMenu]);

    async function salvarProduto(product: Product) {
        try {
            if (editingProduct) {
                await updateProduct(editingProduct.id, product);
                sucesso("Produto atualizado.");
            } else {
                await createProduct(product);
                sucesso("Produto cadastrado.");
            }
            carregarProdutos();
        } catch (e) {
            console.error(e);
            mostrarErro("Erro ao salvar.");
        } finally {
            setEditingProduct(null);
        }
    }

    async function salvarCategoria(category: Category) {
        try {
            await createCategory(category);
            sucesso("Categoria cadastrada.");
            carregarCategorias();
        } catch (e) {
            console.error(e);
            mostrarErro("Erro ao cadastrar categoria.");
        }
    }

    async function carregarProdutos() {
        try {
            const dados = await getProducts();
            setProdutos(dados);
        } catch (e) {
            console.error(e);
        }
    }

    async function carregarCategorias() {
        try {
            const dados = await getCategories();
            setCategorias(dados);
        } catch (e) {
            console.error(e);
        }
    }

    function modeDelete(on: boolean) {
        setDeleteMode(on);
        setSelectedProducts([]);
        setSelectedCategories([]);
    }

    function modeCreate(on: boolean) {
        setCreateMode(on);
    }

    function toggleCreateMode() {
        if (deleteMode) modeDelete(false);
        modeCreate(!createMode);
    }

    function toggleProduct(id: string) {
        setSelectedProducts(prev =>
            prev.includes(id)
                ? prev.filter(x => x !== id)
                : [...prev, id]
        );
    }

    function toggleCategory(id: string) {
        setSelectedCategories(prev =>
            prev.includes(id)
                ? prev.filter(x => x !== id)
                : [...prev, id]
        );
    }

    function toggleFiltroCategoria(id: string) {
        setFiltroCategoria(prev => (prev === id ? "" : id));
    }

    function onExcluirClick() {
        if (createMode) modeCreate(false);

        if (!deleteMode) {
            modeDelete(true);
        } else {
            confirmDelete();
        }
    }

    async function confirmDelete() {
        if (selectedProducts.length === 0 && selectedCategories.length === 0) {
            return;
        }

        try {
            // Produtos precisam ser excluídos antes das categorias: uma categoria só
            // pode ser removida se nenhum produto ainda apontar para ela.
            const resultadosProdutos = await Promise.allSettled(
                selectedProducts.map(id => deleteProduct(id))
            );

            const resultadosCategorias = await Promise.allSettled(
                selectedCategories.map(id => deleteCategory(id))
            );

            const produtosExcluidos = selectedProducts.filter(
                (_, i) => resultadosProdutos[i].status === "fulfilled"
            );

            const categoriasExcluidas = selectedCategories.filter(
                (_, i) => resultadosCategorias[i].status === "fulfilled"
            );

            setProdutos(prev =>
                prev.filter(produto => !produtosExcluidos.includes(produto.id))
            );

            setCategorias(prev =>
                prev.filter(categoria => !categoriasExcluidas.includes(categoria.id))
            );

            setSelectedProducts([]);
            setSelectedCategories([]);
            setDeleteMode(false);

            const houveFalha =
                resultadosProdutos.some(r => r.status === "rejected") ||
                resultadosCategorias.some(r => r.status === "rejected");

            if (houveFalha) {
                mostrarErro("Não foi possível excluir um ou mais itens (podem estar em uso).");
            }
        } catch (error) {
            console.error("Erro ao excluir:", error);
            mostrarErro("Não foi possível excluir um ou mais itens (podem estar em uso).");
        }
    }

    async function comprarProduto(produto: Product) {
        try {
            await purchaseProduct(produto.id);
            await refreshUser();
            sucesso(`Você comprou ${produto.nome}.`);
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Não foi possível comprar o produto.");
        }
    }

    function iniciarCriacaoProduto() {
        modeCreate(false);
        setEditingProduct(null);
        setShowModal(true);
    }

    function iniciarCriacaoCategoria() {
        modeCreate(false);
        setShowCategoryModal(true);
    }

    function abrirMenuContexto(e: MouseEvent, produto: Product) {
        e.preventDefault();

        if (deleteMode || createMode || !isAdmin) return;

        setContextMenu({ x: e.clientX, y: e.clientY, produto });
    }

    function editarProduto() {
        if (!contextMenu) return;

        setEditingProduct(contextMenu.produto);
        setShowModal(true);
        setContextMenu(null);
    }

    const produtosFiltrados = produtos
        .filter(x => x.nome.toLowerCase().includes(search.toLowerCase()))
        .filter(x => !filtroCategoria || x.categoria === filtroCategoria);

    return {
        isAdmin,

        search,
        setSearch,
        filtroCategoria,
        toggleFiltroCategoria,

        produtos,
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
        comprarProduto,

        contextMenu,
        abrirMenuContexto,
        editarProduto,

        salvarProduto,
        salvarCategoria,
    };
}
