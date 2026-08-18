"use client";

import { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { getClipes, criarClipe, curtirClipe, excluirClipe } from "../services/clipesApi";
import { vincularYoutube, desvincularYoutube } from "../services/authApi";
import Link from "next/link";
import { Clipe, ClipeVencedor, CreateClipeRequest } from "../models/Clipe";
import ClipeModal from "./components/ClipeModal";
import EstadoVazio from "../components/EstadoVazio";
import "./page.css";

function extrairYoutubeId(url: string): string | null {
    const match = url.match(/(?:youtube\.com\/(?:watch\?v=|embed\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})/);
    return match ? match[1] : null;
}

export default function Clipes() {
    useRequireAuth();

    const { user, refreshUser } = useAuth();

    const [clipes, setClipes] = useState<Clipe[]>([]);
    const [ultimoVencedor, setUltimoVencedor] = useState<ClipeVencedor | null>(null);
    const [proximoFechamento, setProximoFechamento] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const [curtindo, setCurtindo] = useState<string | null>(null);
    const [excluindo, setExcluindo] = useState<string | null>(null);
    const [desvinculando, setDesvinculando] = useState(false);
    const [mensagem, setMensagem] = useState<{ tipo: "sucesso" | "erro"; texto: string } | null>(null);
    const listaRef = useScrollReveal<HTMLDivElement>(clipes.length);

    useEffect(() => {
        carregar();

        const status = new URLSearchParams(window.location.search).get("youtube");

        if (status === "vinculado") {
            setMensagem({ tipo: "sucesso", texto: "Canal do YouTube vinculado com sucesso!" });
            refreshUser();
        } else if (status === "erro") {
            setMensagem({ tipo: "erro", texto: "Não foi possível vincular o canal do YouTube. Tente novamente." });
        }

        if (status) {
            window.history.replaceState(null, "", "/Clipes");
        }
    }, []);

    async function carregar() {
        try {
            const dados = await getClipes();
            setClipes(dados.clipes);
            setProximoFechamento(dados.proximoFechamento);
            setUltimoVencedor(dados.ultimoVencedor);
        } catch (e) {
            console.error(e);
        }
    }

    async function postarClipe(request: CreateClipeRequest) {
        await criarClipe(request);
        await carregar();
    }

    async function curtir(clipe: Clipe) {
        setCurtindo(clipe.id);

        try {
            await curtirClipe(clipe.id);
            await carregar();
        } catch (e) {
            setMensagem({ tipo: "erro", texto: e instanceof Error ? e.message : "Erro ao curtir." });
        } finally {
            setCurtindo(null);
        }
    }

    async function excluir(clipe: Clipe) {
        if (!confirm(`Excluir o clipe "${clipe.titulo}"? Essa ação não pode ser desfeita.`))
            return;

        setExcluindo(clipe.id);

        try {
            await excluirClipe(clipe.id);
            await carregar();
        } catch (e) {
            setMensagem({ tipo: "erro", texto: e instanceof Error ? e.message : "Erro ao excluir clipe." });
        } finally {
            setExcluindo(null);
        }
    }

    async function desvincular() {
        if (!confirm("Desvincular seu canal do YouTube? Você vai precisar vincular de novo pra postar clipes."))
            return;

        setDesvinculando(true);

        try {
            await desvincularYoutube();
            await refreshUser();
        } catch (e) {
            setMensagem({ tipo: "erro", texto: e instanceof Error ? e.message : "Erro ao desvincular." });
        } finally {
            setDesvinculando(false);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR");
    }

    function formatarPrazo(data: string) {
        const diff = new Date(data).getTime() - Date.now();

        if (diff <= 0) return "encerrando...";

        const dias = Math.floor(diff / (1000 * 60 * 60 * 24));
        const horas = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

        if (dias > 0) return `${dias}d ${horas}h`;
        return `${horas}h`;
    }

    const vinculado = !!user?.youtubeChannelNome;
    const isAdmin = user?.isAdmin ?? false;

    return (
        <main className="containerClipes">
            <div className="clipesHeader">
                <div>
                    <h2 className="section-title">Clipe da Semana</h2>
                    <p className="clipesSubtitulo">
                        O clipe mais curtido da semana leva 500 Az Coins pro autor. Em caso de empate, sorteio decide.
                        {proximoFechamento && <> Encerra em <strong>{formatarPrazo(proximoFechamento)}</strong>.</>}
                    </p>
                </div>

                {vinculado ? (
                    <button className="btnPostarClipe" onClick={() => setShowModal(true)}>
                        Postar Clipe
                    </button>
                ) : (
                    <div className="clipesVincularWrap">
                        <button className="btnVincularYoutube" onClick={vincularYoutube}>
                            Vincular canal do YouTube
                        </button>
                        <span className="clipesVincularAviso">
                            Só lemos o ID do seu canal, pra confirmar dono do clipe. Veja nossa{" "}
                            <Link href="/Privacidade">Política de Privacidade</Link>.
                        </span>
                    </div>
                )}
            </div>

            {vinculado && (
                <p className="clipesCanalVinculado">
                    Canal vinculado: <strong>{user!.youtubeChannelNome}</strong>
                    <button className="btnDesvincularYoutube" disabled={desvinculando} onClick={desvincular}>
                        {desvinculando ? "Desvinculando..." : "Desvincular"}
                    </button>
                </p>
            )}

            {mensagem && (
                <div className={`clipesAviso clipesAviso-${mensagem.tipo}`}>
                    {mensagem.texto}
                </div>
            )}

            {ultimoVencedor && (
                <div className="vencedorAnterior">
                    <div className="vencedorAnteriorSelo">🏆 Vencedor da semana passada</div>

                    <div className="vencedorAnteriorConteudo">
                        <div className="clipe-player vencedorAnteriorPlayer">
                            {(() => {
                                const youtubeId = extrairYoutubeId(ultimoVencedor.url);
                                return youtubeId ? (
                                    <iframe
                                        src={`https://www.youtube.com/embed/${youtubeId}`}
                                        title={ultimoVencedor.titulo}
                                        allowFullScreen
                                    />
                                ) : (
                                    <a className="clipe-link-externo" href={ultimoVencedor.url} target="_blank" rel="noreferrer">
                                        Assistir clipe ↗
                                    </a>
                                );
                            })()}
                        </div>

                        <div className="vencedorAnteriorInfo">
                            <span className="clipe-titulo">{ultimoVencedor.titulo}</span>

                            <div className="clipe-autor">
                                {ultimoVencedor.autorAvatar && <img src={ultimoVencedor.autorAvatar} alt={ultimoVencedor.autorNome} />}
                                <span>{ultimoVencedor.autorNome}</span>
                            </div>

                            <span className="vencedorAnteriorDetalhe">
                                🤍 {ultimoVencedor.curtidas} curtidas · encerrado em {formatarData(ultimoVencedor.fechadoEm)}
                            </span>
                        </div>
                    </div>
                </div>
            )}

            {clipes.length === 0 && (
                <EstadoVazio
                    icone="🎬"
                    titulo="Nenhum clipe postado essa semana ainda."
                    descricao="Seja o primeiro a postar e concorrer aos 500 Az Coins do vencedor."
                />
            )}

            <div className="lista-clipes" ref={listaRef}>
                {clipes.map((clipe, index) => {
                    const youtubeId = extrairYoutubeId(clipe.url);

                    return (
                        <div
                            key={clipe.id}
                            className={`clipe-card reveal ${index === 0 && clipe.curtidas > 0 ? "clipe-lider" : ""}`}
                            style={{ transitionDelay: `${Math.min(index, 8) * 45}ms` }}
                        >
                            <div className="clipe-ranking">
                                {index === 0 && clipe.curtidas > 0 ? "🏆" : `#${index + 1}`}
                            </div>

                            {(clipe.autorSouEu || isAdmin) && (
                                <button
                                    className="btnExcluirClipe"
                                    title="Excluir clipe"
                                    disabled={excluindo === clipe.id}
                                    onClick={() => excluir(clipe)}
                                >
                                    ✕
                                </button>
                            )}

                            <div className="clipe-player">
                                {youtubeId ? (
                                    <iframe
                                        src={`https://www.youtube.com/embed/${youtubeId}`}
                                        title={clipe.titulo}
                                        allowFullScreen
                                    />
                                ) : (
                                    <a className="clipe-link-externo" href={clipe.url} target="_blank" rel="noreferrer">
                                        Assistir clipe ↗
                                    </a>
                                )}
                            </div>

                            <div className="clipe-info">
                                <span className="clipe-titulo">{clipe.titulo}</span>

                                <div className="clipe-autor">
                                    {clipe.autorAvatar && <img src={clipe.autorAvatar} alt={clipe.autorNome} />}
                                    <span>{clipe.autorNome}</span>
                                </div>
                            </div>

                            <button
                                className={`btnCurtir ${clipe.jaCurti ? "btnCurtir-ativo" : ""}`}
                                disabled={clipe.autorSouEu || clipe.jaCurti || curtindo === clipe.id}
                                title={clipe.autorSouEu ? "Você não pode curtir seu próprio clipe" : undefined}
                                onClick={() => curtir(clipe)}
                            >
                                {clipe.jaCurti ? "❤" : "🤍"} {clipe.curtidas}
                            </button>
                        </div>
                    );
                })}
            </div>

            {showModal && (
                <ClipeModal
                    onClose={() => setShowModal(false)}
                    onSave={postarClipe}
                />
            )}
        </main>
    );
}
