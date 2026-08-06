"use client";

import { useState } from "react";
import { Product } from "../models/Product";
import { createProduct } from "../services/productsApi";
import "./Css/ProductModal.css";


interface Props {
    onClose: () => void;
    onSave: (product: Product) => void;
}

export default function ProductModal({ onClose, onSave }: Props) {

    const [nome, setNome] = useState("");
    const [preco, setPreco] = useState(0);
    const [imagem, setImagem] = useState("");
    const [descricao, setDescricao] = useState("");
    const [estoque, setEstoque] = useState(0);

    function salvar() {

        onSave({
            id: 0,
            nome,
            descricao,
            preco,
            estoque,
            imagem
        });

        onClose();
    }

    return (

        <div className="modal-overlay">

            <div className="modal">

                <h2>Novo Produto</h2>

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