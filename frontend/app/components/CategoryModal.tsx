"use client";

import { useState } from "react";
import { Category } from "../models/Category";
import "./Modal.css";

interface Props {
    onClose: () => void;
    onSave: (category: Category) => void;
}

export default function CategoryModal({ onClose, onSave }: Props) {

    const [nome, setNome] = useState("");

    function salvar() {
        if (!nome.trim()) return;

        onSave({
            id: '',
            nome
        });

        onClose();
    }

    return (

        <div className="modal-overlay">

            <div className="modal modal-small">

                <h2>Nova Categoria</h2>

                <div className="modal-field">
                    <label htmlFor="categoria-nome">Nome</label>
                    <input
                        id="categoria-nome"
                        placeholder="Nome"
                        value={nome}
                        onChange={(e) => setNome(e.target.value)}
                    />
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
