"use client";

import { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { getClipes, criarClipe, curtirClipe } from "../services/clipesApi";
import { vincularYoutube } from "../services/authApi";
import { Clipe, CreateClipeRequest } from "../models/Clipe";
import ClipeModal from "./components/ClipeModal";
import "./page.css";

function extrairYoutubeId(url: string): string | null {
    const match = url.match(/(?:youtube\.com\/(?:watch\?v=|embed\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})/);
    return match ? match[1] : null;
}

export default function Clipes() {
    useRequireAuth();

    const { user, refreshUser } = useAuth();

    const [clipes, setClipes] = useState<Clipe[]>([]);
    const [proximoFechamento, setProximoFechamento] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const [curtindo, setCurtindo] = useState<string | null>(null);
    const [mensagem, setMensagem] = useState<{ tipo: "sucesso" | "erro"; texto: string } | null>(null);

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

    function formatarPrazo(data: string) {
        const diff = new Date(data).getTime() - Date.now();

        if (diff <= 0) return "encerrando...";

        const dias = Math.floor(diff / (1000 * 60 * 60 * 24));
        const horas = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

        if (dias > 0) return `${dias}d ${horas}h`;
        return `${horas}h`;
    }

    const vinculado = !!user?.youtubeChannelNome;

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
                    <button className="btnVincularYoutube" onClick={vincularYoutube}>
                        Vincular canal do YouTube
                    </button>
                )}
            </div>

            {vinculado && (
                <p className="clipesCanalVinculado">Canal vinculado: <strong>{user!.youtubeChannelNome}</strong></p>
            )}

            {mensagem && (
                <div className={`clipesAviso clipesAviso-${mensagem.tipo}`}>
                    {mensagem.texto}
                </div>
            )}

            {clipes.length === 0 && (
                <p className="clipesVazio">Nenhum clipe postado essa semana ainda.</p>
            )}

            <div className="lista-clipes">
                {clipes.map((clipe, index) => {
                    const youtubeId = extrairYoutubeId(clipe.url);

                    return (
                        <div key={clipe.id} className={`clipe-card ${index === 0 && clipe.curtidas > 0 ? "clipe-lider" : ""}`}>
                            <div className="clipe-ranking">
                                {index === 0 && clipe.curtidas > 0 ? "🏆" : `#${index + 1}`}
                            </div>

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
