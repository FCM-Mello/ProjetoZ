"use client";

import { useEffect, useMemo, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { getRanking, resetarRanking } from "../services/rankingApi";
import { RankingJogador } from "../models/Ranking";
import Skeleton from "../components/Skeleton";
import EstadoVazio from "../components/EstadoVazio";
import "./page.css";

type Aba = "kd" | "koth" | "zumbis" | "tempo";

const ABAS: { id: Aba; label: string }[] = [
    { id: "kd", label: "Maior K/D" },
    { id: "koth", label: "Mais KOTH" },
    { id: "zumbis", label: "Mais Zumbis Mortos" },
    { id: "tempo", label: "Mais Tempo de Sobrevivência" },
];

function formatarTempo(segundos: number) {
    const horas = Math.floor(segundos / 3600);
    const minutos = Math.floor((segundos % 3600) / 60);

    return horas > 0 ? `${horas}h ${minutos}min` : `${minutos}min`;
}

export default function Ranking() {
    useRequireAuth();

    const { user } = useAuth();
    const isAdmin = user?.isAdmin ?? false;

    const [jogadores, setJogadores] = useState<RankingJogador[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [aba, setAba] = useState<Aba>("kd");
    const [resetando, setResetando] = useState(false);
    const listaRef = useScrollReveal<HTMLDivElement>(jogadores.length);

    useEffect(() => {
        carregar();
    }, []);

    async function carregar() {
        setCarregando(true);
        try {
            setJogadores(await getRanking());
        } catch (e) {
            console.error(e);
        } finally {
            setCarregando(false);
        }
    }

    const ordenados = useMemo(() => {
        const copia = [...jogadores];

        switch (aba) {
            case "koth":
                return copia.sort((a, b) => b.kothCompletados - a.kothCompletados || b.kd - a.kd);
            case "zumbis":
                return copia.sort((a, b) => b.zumbiKills - a.zumbiKills || b.kd - a.kd);
            case "tempo":
                return copia.sort((a, b) => b.segundosJogados - a.segundosJogados || b.kd - a.kd);
            default:
                return copia.sort((a, b) => b.kd - a.kd || b.kothCompletados - a.kothCompletados);
        }
    }, [jogadores, aba]);

    async function resetar() {
        if (!confirm("Resetar o ranking de todos os jogadores? Essa ação não pode ser desfeita."))
            return;

        setResetando(true);
        try {
            await resetarRanking();
            await carregar();
        } catch (e) {
            console.error(e);
        } finally {
            setResetando(false);
        }
    }

    return (
        <main className="containerRanking">
            <div className="rankingHeader">
                <h2 className="section-title">Ranking Global</h2>

                {isAdmin && (
                    <button className="btnResetarRanking" disabled={resetando} onClick={resetar}>
                        {resetando ? "Resetando..." : "Resetar Ranking"}
                    </button>
                )}
            </div>

            <nav className="rankingAbas">
                {ABAS.map(a => (
                    <button
                        key={a.id}
                        className={`rankingAba ${aba === a.id ? "rankingAba-ativa" : ""}`}
                        onClick={() => setAba(a.id)}
                    >
                        {a.label}
                    </button>
                ))}
            </nav>

            {!carregando && ordenados.length === 0 && (
                <EstadoVazio
                    icone="🏆"
                    titulo="Nenhum jogador no ranking ainda."
                    descricao="Assim que o mod sincronizar K/D, KOTH, zumbis ou tempo de jogo, os jogadores aparecem aqui."
                />
            )}

            {carregando && (
                <div className="rankingLista">
                    {[0, 1, 2, 3, 4].map(i => (
                        <div key={i} className="rankingItem">
                            <Skeleton width={24} height={16} />
                            <Skeleton width={36} height={36} borderRadius="50%" />
                            <Skeleton width="30%" height={16} />
                        </div>
                    ))}
                </div>
            )}

            <div className="rankingLista" ref={listaRef}>
                {ordenados.map((jogador, i) => (
                    <div
                        key={jogador.steamId}
                        className="rankingItem reveal"
                        style={{ transitionDelay: `${Math.min(i, 8) * 45}ms` }}
                    >
                        <span className="rankingPosicao">{i + 1}º</span>

                        {jogador.avatar && <img className="rankingAvatar" src={jogador.avatar} alt={jogador.nome} />}

                        <span className="rankingNome">{jogador.nome}</span>

                        <div className="rankingStats">
                            <span className="rankingStat" title="Kills / Mortes">
                                {jogador.kills} / {jogador.deaths}
                            </span>

                            <span className={`rankingStatDestaque ${aba === "kd" ? "rankingStatDestaque-ativa" : ""}`} title="K/D">
                                K/D {jogador.kd.toFixed(2)}
                            </span>

                            <span className={`rankingStatDestaque ${aba === "koth" ? "rankingStatDestaque-ativa" : ""}`} title="KOTH completados">
                                🏆 {jogador.kothCompletados}
                            </span>

                            <span className={`rankingStatDestaque ${aba === "zumbis" ? "rankingStatDestaque-ativa" : ""}`} title="Zumbis mortos">
                                🧟 {jogador.zumbiKills}
                            </span>

                            <span className={`rankingStatDestaque ${aba === "tempo" ? "rankingStatDestaque-ativa" : ""}`} title="Tempo de sobrevivência">
                                ⏱️ {formatarTempo(jogador.segundosJogados)}
                            </span>
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
