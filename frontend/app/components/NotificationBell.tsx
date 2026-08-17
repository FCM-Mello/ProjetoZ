"use client";

import { useEffect, useRef, useState } from "react";
import { getMinhasNotificacoes, marcarLida, marcarTodasLidas } from "../services/notificacoesApi";
import { Notificacao } from "../models/Notificacao";
import { useClickOutside } from "../hooks/useClickOutside";
import "./NotificationBell.css";

const INTERVALO_ATUALIZACAO_MS = 60_000;

export default function NotificationBell() {
    const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
    const [aberto, setAberto] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);

    useClickOutside(menuRef, () => setAberto(false));

    async function carregar() {
        try {
            setNotificacoes(await getMinhasNotificacoes());
        } catch (e) {
            console.error(e);
        }
    }

    useEffect(() => {
        carregar();
        const intervalo = setInterval(carregar, INTERVALO_ATUALIZACAO_MS);
        return () => clearInterval(intervalo);
    }, []);

    const naoLidas = notificacoes.filter(n => !n.lida).length;

    async function abrirNotificacao(notificacao: Notificacao) {
        if (!notificacao.lida) {
            setNotificacoes(atual => atual.map(n => n.id === notificacao.id ? { ...n, lida: true } : n));
            try {
                await marcarLida(notificacao.id);
            } catch (e) {
                console.error(e);
            }
        }
    }

    async function marcarTudoComoLido() {
        setNotificacoes(atual => atual.map(n => ({ ...n, lida: true })));
        try {
            await marcarTodasLidas();
        } catch (e) {
            console.error(e);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
    }

    return (
        <div className="notificationBell" ref={menuRef}>
            <button className="notificationBellBotao" onClick={() => setAberto(a => !a)} title="Notificações">
                🔔
                {naoLidas > 0 && <span className="notificationBellBadge">{naoLidas > 9 ? "9+" : naoLidas}</span>}
            </button>

            {aberto && (
                <div className="notificationDropdown">
                    <div className="notificationDropdownTopo">
                        <span>Notificações</span>
                        {naoLidas > 0 && (
                            <button className="notificationMarcarTudo" onClick={marcarTudoComoLido}>
                                Marcar tudo como lido
                            </button>
                        )}
                    </div>

                    {notificacoes.length === 0 ? (
                        <p className="notificationVazio">Nenhuma notificação.</p>
                    ) : (
                        <ul className="notificationLista">
                            {notificacoes.map(n => (
                                <li
                                    key={n.id}
                                    className={`notificationItem notificationItem-${n.nivel} ${n.lida ? "" : "notificationItem-naoLida"}`}
                                    onClick={() => abrirNotificacao(n)}
                                >
                                    <strong>{n.titulo}</strong>
                                    <p>{n.mensagem}</p>
                                    <span className="notificationData">{formatarData(n.enviarEm)}</span>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
}
