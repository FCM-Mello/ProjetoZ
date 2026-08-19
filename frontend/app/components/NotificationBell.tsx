"use client";

import { useEffect, useRef, useState } from "react";
import { getMinhasNotificacoes, marcarLida, marcarTodasLidas } from "../services/notificacoesApi";
import { aceitarConviteCla, recusarConviteCla } from "../services/clasApi";
import { Notificacao } from "../models/Notificacao";
import { useClickOutside } from "../hooks/useClickOutside";
import { useToast } from "../contexts/ToastContext";
import EstadoVazio from "./EstadoVazio";
import "./NotificationBell.css";

const INTERVALO_ATUALIZACAO_MS = 60_000;

export default function NotificationBell() {
    const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
    const [aberto, setAberto] = useState(false);
    const [pingando, setPingando] = useState(false);
    const [respondendo, setRespondendo] = useState<string | null>(null);
    const menuRef = useRef<HTMLDivElement>(null);
    const naoLidasAnteriorRef = useRef(0);
    const { sucesso, erro: mostrarErro } = useToast();

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

    // Só "pinga" quando o número de não lidas SOBE (notificação nova chegando
    // no polling) — marcar como lida também muda naoLidas, mas pra baixo, e
    // isso não deve disparar o ping.
    useEffect(() => {
        if (naoLidas > naoLidasAnteriorRef.current) {
            setPingando(true);
            const timeout = setTimeout(() => setPingando(false), 700);
            naoLidasAnteriorRef.current = naoLidas;
            return () => clearTimeout(timeout);
        }

        naoLidasAnteriorRef.current = naoLidas;
    }, [naoLidas]);

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

    async function responderConvite(notificacao: Notificacao, aceitar: boolean, evento: React.MouseEvent) {
        evento.stopPropagation();

        if (!notificacao.claConviteId) return;

        setRespondendo(notificacao.id);
        try {
            if (aceitar) {
                await aceitarConviteCla(notificacao.claConviteId);
                sucesso("Você entrou no clã!");
            } else {
                await recusarConviteCla(notificacao.claConviteId);
            }

            setNotificacoes(atual => atual.filter(n => n.id !== notificacao.id));
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao responder o convite.");
        } finally {
            setRespondendo(null);
        }
    }

    return (
        <div className="notificationBell" ref={menuRef}>
            <button className="notificationBellBotao" onClick={() => setAberto(a => !a)} title="Notificações">
                🔔
                {naoLidas > 0 && (
                    <span className={`notificationBellBadge ${pingando ? "notificationBellBadge-ping" : ""}`}>
                        {naoLidas > 9 ? "9+" : naoLidas}
                    </span>
                )}
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
                        <EstadoVazio icone="🔔" titulo="Nenhuma notificação." compacto />
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

                                    {n.tipo === "convite_cla" && n.claConviteId && (
                                        <div className="notificationAcoes">
                                            <button
                                                className="notificationBtnAceitar"
                                                disabled={respondendo === n.id}
                                                onClick={(e) => responderConvite(n, true, e)}
                                            >
                                                Aceitar
                                            </button>
                                            <button
                                                className="notificationBtnRecusar"
                                                disabled={respondendo === n.id}
                                                onClick={(e) => responderConvite(n, false, e)}
                                            >
                                                Recusar
                                            </button>
                                        </div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
}
