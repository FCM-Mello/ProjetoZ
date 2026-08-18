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

type Criterio = "kd" | "koth";

export default function Ranking() {
    useRequireAuth();

    const { user } = useAuth();
    const isAdmin = user?.isAdmin ?? false;

    const [jogadores, setJogadores] = useState<RankingJogador[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [criterio, setCriterio] = useState<Criterio>("kd");
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

        return criterio === "koth"
            ? copia.sort((a, b) => b.kothCompletados - a.kothCompletados || b.kd - a.kd)
            : copia.sort((a, b) => b.kd - a.kd || b.kothCompletados - a.kothCompletados);
    }, [jogadores, criterio]);

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

            <div className="rankingFiltros">
                <button
                    className={`rankingFiltro ${criterio === "kd" ? "rankingFiltro-ativo" : ""}`}
                    onClick={() => setCriterio("kd")}
                >
                    Maior K/D
                </button>
                <button
                    className={`rankingFiltro ${criterio === "koth" ? "rankingFiltro-ativo" : ""}`}
                    onClick={() => setCriterio("koth")}
                >
                    Mais KOTH completados
                </button>
            </div>

            {!carregando && ordenados.length === 0 && (
                <EstadoVazio
                    icone="🏆"
                    titulo="Nenhum jogador no ranking ainda."
                    descricao="Assim que o mod sincronizar K/D ou KOTH, os jogadores aparecem aqui."
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
                            <span className="rankingStatDestaque" title="K/D">
                                K/D {jogador.kd.toFixed(2)}
                            </span>
                            <span className="rankingStatDestaque" title="KOTH completados">
                                🏆 {jogador.kothCompletados}
                            </span>
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
