"use client";

import { useState } from "react";
import { CreateClipeRequest } from "../../models/Clipe";
import "../../components/Modal.css";

interface Props {
    onClose: () => void;
    onSave: (request: CreateClipeRequest) => Promise<void>;
}

export default function ClipeModal({ onClose, onSave }: Props) {
    const [titulo, setTitulo] = useState("");
    const [url, setUrl] = useState("");
    const [enviando, setEnviando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);

    async function salvar() {
        if (!titulo.trim() || !url.trim()) return;

        setEnviando(true);
        setErro(null);

        try {
            await onSave({ titulo: titulo.trim(), url: url.trim() });
            onClose();
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Erro ao postar clipe.");
        } finally {
            setEnviando(false);
        }
    }

    return (
        <div className="modal-overlay">
            <div className="modal modal-small">
                <h2>Postar Clipe</h2>

                {erro && <div className="admin-modal-erro">{erro}</div>}

                <div className="modal-field">
                    <label htmlFor="clipe-titulo">Título</label>
                    <input
                        id="clipe-titulo"
                        placeholder="Nome do clipe"
                        value={titulo}
                        onChange={(e) => setTitulo(e.target.value)}
                    />
                </div>

                <div className="modal-field">
                    <label htmlFor="clipe-url">Link do YouTube</label>
                    <input
                        id="clipe-url"
                        placeholder="https://www.youtube.com/watch?v=..."
                        value={url}
                        onChange={(e) => setUrl(e.target.value)}
                    />
                </div>

                <div className="modal-buttons">
                    <button className="btnCancel" onClick={onClose}>Cancelar</button>
                    <button className="btnSave" disabled={enviando} onClick={salvar}>
                        {enviando ? "Postando..." : "Postar"}
                    </button>
                </div>
            </div>
        </div>
    );
}
