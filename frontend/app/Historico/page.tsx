"use client";

import { useEffect, useState } from "react";
import { getMinhasCompras } from "../services/comprasApi";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { Compra } from "../models/Compra";
import "./page.css";

export default function Historico() {
    useRequireAuth();

    const [compras, setCompras] = useState<Compra[]>([]);
    const [carregando, setCarregando] = useState(true);

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

            <div className="lista-compras">
                {compras.map(compra => (
                    <div key={compra.id} className="compra-item">
                        <span className={`compra-tipo tipo-${compra.tipo}`}>
                            {compra.tipo === "produto" ? "Produto" : compra.tipo === "coins" ? "Coins" : compra.tipo === "vip" ? "VIP" : compra.tipo === "mod" ? "In-game" : "Sorteio"}
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
                        </div>
                    </div>
                ))}
            </div>
        </main>
    );
}
