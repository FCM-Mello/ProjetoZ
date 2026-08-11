"use client";

import { useState } from "react";
import { Product } from "../models/Product";
import { Category } from "../models/Category";
import "./Modal.css";


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

    const editando = !!product;

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

                <input
                    placeholder="Nome"
                    value={nome}
                    onChange={(e)=>setNome(e.target.value)}
                />

<textarea
    placeholder="Descrição"
    value={descricao}
    onChange={(e) => setDescricao(e.target.value)}
/>

                <select
                    value={categoria}
                    onChange={(e) => setCategoria(e.target.value)}
                >
                    <option value="">Selecione uma categoria</option>
                    {categorias.map(cat => (
                        <option key={cat.id} value={cat.id}>{cat.nome}</option>
                    ))}
                </select>

                <input
                    type="number"
                    placeholder="Preço"
                    value={preco}
                    onChange={(e)=>setPreco(Number(e.target.value))}
                />

<input
    type="number"
    placeholder="Estoque"
    value={estoque}
    onChange={(e) => setEstoque(Number(e.target.value))}
/>

                <input
                    placeholder="Imagem"
                    value={imagem}
                    onChange={(e)=>setImagem(e.target.value)}
                />

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
