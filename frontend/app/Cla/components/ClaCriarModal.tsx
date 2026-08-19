"use client";

import { useRef, useState, type DragEvent } from "react";
import { useToast } from "../../contexts/ToastContext";
import { CriarClaRequest } from "../../models/Cla";
import "../../components/Modal.css";

const TAMANHO_MAXIMO_BYTES = 2 * 1024 * 1024; // 2MB

interface Props {
    onClose: () => void;
    onSave: (request: CriarClaRequest) => void;
}

export default function ClaCriarModal({ onClose, onSave }: Props) {
    const [nome, setNome] = useState("");
    const [descricao, setDescricao] = useState("");
    const [estandarte, setEstandarte] = useState("");
    const [arrastando, setArrastando] = useState(false);

    const { erro: mostrarErro } = useToast();
    const inputArquivoRef = useRef<HTMLInputElement>(null);

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
            setEstandarte(base64);
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
        if (nome.trim().length < 3) {
            mostrarErro("O nome do clã precisa ter pelo menos 3 caracteres.");
            return;
        }

        onSave({ nome: nome.trim(), descricao: descricao.trim(), estandarte: estandarte || null });
    }

    return (
        <div className="modal-overlay">
            <div className="modal modal-small">
                <h2>Criar Clã</h2>

                <div className="modal-field">
                    <label htmlFor="cla-nome">Nome</label>
                    <input
                        id="cla-nome"
                        placeholder="Nome do clã"
                        value={nome}
                        onChange={(e) => setNome(e.target.value)}
                        maxLength={40}
                    />
                </div>

                <div className="modal-field">
                    <label htmlFor="cla-descricao">Descrição</label>
                    <textarea
                        id="cla-descricao"
                        placeholder="Descrição (opcional)"
                        value={descricao}
                        onChange={(e) => setDescricao(e.target.value)}
                        maxLength={300}
                    />
                </div>

                <div className="modal-field">
                    <label>Estandarte</label>
                    <div
                        className={`dropzone ${arrastando ? "dropzone-ativo" : ""}`}
                        onDragOver={(e) => { e.preventDefault(); setArrastando(true); }}
                        onDragLeave={() => setArrastando(false)}
                        onDrop={onDrop}
                        onClick={() => inputArquivoRef.current?.click()}
                    >
                        {estandarte ? (
                            <img src={estandarte} alt="Prévia do estandarte" className="dropzone-preview" />
                        ) : (
                            <span className="dropzone-texto">
                                Arraste uma imagem aqui ou clique para escolher (máx. 2MB, opcional)
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
                    <button className="btnCancel" onClick={onClose}>Cancelar</button>
                    <button className="btnSave" onClick={salvar}>Criar</button>
                </div>
            </div>
        </div>
    );
}
