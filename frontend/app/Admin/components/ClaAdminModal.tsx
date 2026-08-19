"use client";

import { useEffect, useState } from "react";
import { getClaAdmin, removerMembroClaAdmin, desfazerClaAdmin } from "../../services/adminApi";
import { AdminClaDetalhe } from "../../models/AdminCla";
import "../../components/Modal.css";
import "./UsuarioAdminModal.css";

interface Props {
    claId: string;
    onClose: () => void;
    onChange: () => void;
}

export default function ClaAdminModal({ claId, onClose, onChange }: Props) {
    const [cla, setCla] = useState<AdminClaDetalhe | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [processando, setProcessando] = useState<string | null>(null);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        carregar();
    }, [claId]);

    async function carregar() {
        setCarregando(true);
        try {
            setCla(await getClaAdmin(claId));
        } catch (e) {
            console.error(e);
            setErro("Não foi possível carregar o clã.");
        } finally {
            setCarregando(false);
        }
    }

    async function removerMembro(userId: string, nome: string) {
        if (!confirm(`Remover ${nome} do clã?`)) return;

        setErro(null);
        setProcessando(userId);

        try {
            await removerMembroClaAdmin(claId, userId);
            await carregar();
            onChange();
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Erro ao remover membro.");
        } finally {
            setProcessando(null);
        }
    }

    async function desfazer() {
        if (!cla) return;
        if (!confirm(`Desfazer o clã "${cla.nome}"? Essa ação não pode ser desfeita.`)) return;

        setErro(null);
        setProcessando("desfazer");

        try {
            await desfazerClaAdmin(claId);
            onChange();
            onClose();
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Erro ao desfazer clã.");
            setProcessando(null);
        }
    }

    return (
        <div className="modal-overlay">
            <div className="modal admin-usuario-modal">
                {carregando || !cla ? (
                    <p className="admin-modal-carregando">Carregando...</p>
                ) : (
                    <>
                        <div className="admin-modal-header">
                            {cla.estandarte
                                ? <img src={cla.estandarte} alt={cla.nome} className="admin-modal-avatar" />
                                : <span className="admin-modal-avatar admin-modal-avatar-vazio">🛡️</span>}

                            <div>
                                <h2 className="admin-modal-nome">{cla.nome}</h2>
                                <span className="admin-modal-steamid">
                                    {cla.totalMembros} {cla.totalMembros === 1 ? "membro" : "membros"}
                                    {cla.grupoModId && " · sincronizado do jogo"}
                                </span>
                            </div>
                        </div>

                        {erro && <div className="admin-modal-erro">{erro}</div>}

                        <div className="admin-secao">
                            <h3>Membros</h3>

                            <ul className="admin-lista-inventario">
                                {cla.membros.map(m => (
                                    <li key={m.userId ?? m.steamId}>
                                        <span>
                                            {m.nome}
                                            {m.isLider && " — Líder"}
                                            {!m.isLider && m.isAdmin && " — Admin"}
                                        </span>

                                        {!m.isLider && m.userId && (
                                            <button
                                                className="admin-btn-perigo"
                                                disabled={processando === m.userId}
                                                onClick={() => removerMembro(m.userId!, m.nome)}
                                            >
                                                Remover
                                            </button>
                                        )}
                                    </li>
                                ))}
                            </ul>
                        </div>

                        <div className="admin-secao">
                            <h3>Desfazer clã</h3>
                            <p className="admin-lista-vazia">Remove o clã e todos os membros, solicitações e convites pendentes.</p>

                            <div className="admin-linha">
                                <button
                                    className="admin-btn-perigo"
                                    disabled={processando === "desfazer"}
                                    onClick={desfazer}
                                >
                                    Desfazer clã
                                </button>
                            </div>
                        </div>
                    </>
                )}

                <div className="modal-buttons">
                    <button className="btnCancel" onClick={onClose}>Fechar</button>
                </div>
            </div>
        </div>
    );
}
