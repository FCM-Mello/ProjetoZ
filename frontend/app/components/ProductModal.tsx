"use client";

import { useRef, useState, type DragEvent } from "react";
import { Product } from "../models/Product";
import { Category } from "../models/Category";
import { useToast } from "../contexts/ToastContext";
import "./Modal.css";

const TAMANHO_MAXIMO_BYTES = 2 * 1024 * 1024; // 2MB

interface Props {
    product?: Product;
    categorias: Category[];
    onClose: () => void;
    onSave: (product: Product) => void;
}

export default function ProductModal({ product, categorias, onClose, onSave }: Props) {

    const [nome, setNome] = useState(product?.nome ?? "");
    const [preco, setPreco] = useState(product?.preco ?? 0);
    const [imagem, setImagem] = useState(product?.imagem ?? "");
    const [descricao, setDescricao] = useState(product?.descricao ?? "");
    const [estoque, setEstoque] = useState(product?.estoque ?? 0);
    const [categoria, setCategoria] = useState(product?.categoria ?? "");
    const [arrastando, setArrastando] = useState(false);

    const { erro: mostrarErro } = useToast();
    const inputArquivoRef = useRef<HTMLInputElement>(null);

    const editando = !!product;

    function lerArquivoComoBase64(file: File): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result as string);
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(file);
        });
    }

    async function processarArquivo(file: File | undefined) {
        if (!file) return;

        if (!file.type.startsWith("image/")) {
            mostrarErro("Selecione um arquivo de imagem.");
            return;
        }

        if (file.size > TAMANHO_MAXIMO_BYTES) {
            mostrarErro("Imagem muito grande. Tamanho máximo: 2MB.");
            return;
        }

        try {
            const base64 = await lerArquivoComoBase64(file);
            setImagem(base64);
        } catch (e) {
            console.error(e);
            mostrarErro("Não foi possível ler a imagem.");
        }
    }

    function onDrop(e: DragEvent<HTMLDivElement>) {
        e.preventDefault();
        setArrastando(false);
        processarArquivo(e.dataTransfer.files?.[0]);
    }

    function salvar() {

        onSave({
            id: product?.id ?? '',
            nome,
            descricao,
            preco,
            estoque,
            imagem,
            categoria
        });

        onClose();
    }

    return (

        <div className="modal-overlay">

            <div className="modal">

                <h2>{editando ? "Editar Produto" : "Novo Produto"}</h2>

                <div className="modal-field">
                    <label htmlFor="produto-nome">Nome</label>
                    <input
                        id="produto-nome"
                        placeholder="Nome"
                        value={nome}
                        onChange={(e) => setNome(e.target.value)}
                    />
                </div>

                <div className="modal-field">
                    <label htmlFor="produto-descricao">Descrição</label>
                    <textarea
                        id="produto-descricao"
                        placeholder="Descrição"
                        value={descricao}
                        onChange={(e) => setDescricao(e.target.value)}
                    />
                </div>

                <div className="modal-field">
                    <label htmlFor="produto-categoria">Categoria</label>
                    <select
                        id="produto-categoria"
                        value={categoria}
                        onChange={(e) => setCategoria(e.target.value)}
                    >
                        <option value="">Selecione uma categoria</option>
                        {categorias.map(cat => (
                            <option key={cat.id} value={cat.id}>{cat.nome}</option>
                        ))}
                    </select>
                </div>

                <div className="modal-row">
                    <div className="modal-field">
                        <label htmlFor="produto-preco">Preço (Az Coins)</label>
                        <input
                            id="produto-preco"
                            type="number"
                            placeholder="Preço"
                            value={preco}
                            onChange={(e) => setPreco(Number(e.target.value))}
                        />
                    </div>

                    <div className="modal-field">
                        <label htmlFor="produto-estoque">Estoque</label>
                        <input
                            id="produto-estoque"
                            type="number"
                            placeholder="Estoque"
                            value={estoque}
                            onChange={(e) => setEstoque(Number(e.target.value))}
                        />
                    </div>
                </div>

                <div className="modal-field">
                    <label>Imagem</label>
                    <div
                        className={`dropzone ${arrastando ? "dropzone-ativo" : ""}`}
                        onDragOver={(e) => { e.preventDefault(); setArrastando(true); }}
                        onDragLeave={() => setArrastando(false)}
                        onDrop={onDrop}
                        onClick={() => inputArquivoRef.current?.click()}
                    >
                        {imagem ? (
                            <img src={imagem} alt="Prévia da imagem" className="dropzone-preview" />
                        ) : (
                            <span className="dropzone-texto">
                                Arraste uma imagem aqui ou clique para escolher (máx. 2MB)
                            </span>
                        )}

                        <input
                            ref={inputArquivoRef}
                            type="file"
                            accept="image/*"
                            className="dropzone-input"
                            onChange={(e) => processarArquivo(e.target.files?.[0])}
                        />
                    </div>
                </div>

                <div className="modal-buttons">

                    <button
                        className="btnCancel"
                        onClick={onClose}>
                        Cancelar
                    </button>

                    <button
                        className="btnSave"
                        onClick={salvar}>
                        Salvar
                    </button>

                </div>

            </div>

        </div>

    );
}
