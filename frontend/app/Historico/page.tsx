"use client";

import { useEffect, useState } from "react";
import { getMinhasCompras } from "../services/comprasApi";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { Compra } from "../models/Compra";
import Skeleton from "../components/Skeleton";
import "./page.css";

const ROTULOS_TIPO: Record<Compra["tipo"], string> = {
    produto: "Produto",
    coins: "Coins",
    sorteio: "Sorteio",
    vip: "VIP",
    mod: "In-game",
    clipe: "Clipe da Semana",
};

export default function Historico() {
    useRequireAuth();

    const [compras, setCompras] = useState<Compra[]>([]);
    const [carregando, setCarregando] = useState(true);
    const listaRef = useScrollReveal<HTMLDivElement>(compras.length);

    useEffect(() => {
        carregar();
    }, []);

    async function carregar() {
        try {
            const dados = await getMinhasCompras();
            setCompras(dados);
        } catch (e) {
            console.error(e);
        } finally {
            setCarregando(false);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleString("pt-BR", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        });
    }

    return (
        <main className="containerHistorico">
            <h2 className="section-title">Histórico de Compras</h2>

            {!carregando && compras.length === 0 && (
                <p className="historicoVazio">Você ainda não fez nenhuma compra.</p>
            )}

            {carregando && (
                <div className="lista-compras">
                    {[0, 1, 2].map(i => (
                        <div key={i} className="compra-item">
                            <Skeleton width={70} height={24} borderRadius={20} />
                            <Skeleton width="40%" height={16} />
                        </div>
                    ))}
                </div>
            )}

            <div className="lista-compras" ref={listaRef}>
                {compras.map((compra, i) => (
                    <div
                        key={compra.id}
                        className="compra-item reveal"
                        style={{ transitionDelay: `${Math.min(i, 8) * 45}ms` }}
                    >
                        <span className={`compra-tipo tipo-${compra.tipo}`}>
                            {ROTULOS_TIPO[compra.tipo]}
                        </span>

                        <div className="compra-info">
                            <span className="compra-descricao">{compra.descricao}</span>
                            <span className="compra-data">{formatarData(compra.criadoEm)}</span>
                        </div>

                        <div className="compra-valores">
                            {compra.tipo === "produto" && (
                                <span className="compra-coins negativo">-🪙 {compra.coins}</span>
                            )}

                            {compra.tipo === "coins" && (
                                <>
                                    <span className="compra-coins positivo">+🪙 {compra.coins}</span>
                                    {compra.valorReais != null && (
                                        <span className="compra-reais">R$ {compra.valorReais.toFixed(2)}</span>
                                    )}
                                </>
                            )}

                            {compra.tipo === "sorteio" && (
                                <span className="compra-coins positivo">🏆 Prêmio ganho</span>
                            )}

                            {compra.tipo === "vip" && (
                                <span className="compra-coins negativo">-🪙 {compra.coins}</span>
                            )}

                            {compra.tipo === "mod" && (
                                <span className="compra-coins negativo">-🪙 {compra.coins}</span>
                            )}

                            {compra.tipo === "clipe" && (
                                <span className="compra-coins positivo">🏆 +🪙 {compra.coins}</span>
                            )}
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
