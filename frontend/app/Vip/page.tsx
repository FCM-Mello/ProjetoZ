"use client";

import { useEffect, useState } from "react";
import { getVipTiers, comprarVip } from "../services/vipApi";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { VipTier } from "../models/VipTier";
import "./page.css";

const BENEFICIOS: Record<number, string[]> = {
    1: ["Acesso à loja VIP", "Prioridade na fila do servidor", "Marcação especial no chat"],
    2: ["Tudo do Bronze", "Kit de loot extra ao spawnar", "Slot de heli reservado"],
    3: ["Tudo da Prata", "Base extra protegida", "Suporte direto com a staff"],
};

const CLASSE_NIVEL: Record<number, string> = {
    1: "tier-bronze",
    2: "tier-prata",
    3: "tier-ouro",
};

export default function Vip() {
    useRequireAuth();

    const { user, refreshUser } = useAuth();

    const [tiers, setTiers] = useState<VipTier[]>([]);
    const [comprando, setComprando] = useState<number | null>(null);
    const [mensagem, setMensagem] = useState<{ tipo: "sucesso" | "erro"; texto: string } | null>(null);

    useEffect(() => {
        carregarTiers();
    }, []);

    async function carregarTiers() {
        try {
            const dados = await getVipTiers();
            setTiers(dados);
        } catch (e) {
            console.error(e);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR");
    }

    async function comprar(nivel: number) {
        setComprando(nivel);
        setMensagem(null);

        try {
            const resultado = await comprarVip(nivel);
            await refreshUser();

            setMensagem({
                tipo: "sucesso",
                texto: `VIP ${resultado.vipNivelNome} ativado até ${formatarData(resultado.vipExpiraEm)}!`,
            });
        } catch (e) {
            setMensagem({
                tipo: "erro",
                texto: e instanceof Error ? e.message : "Não foi possível ativar o VIP.",
            });
        } finally {
            setComprando(null);
        }
    }

    const vipAtivo = (user?.vipNivel ?? 0) > 0;

    return (
        <main className="containerVip">
            <div className="vipBunker">
                <div className="vipScanlines" />
                <div className="vipBunkerGlow" />

                <h2 className="section-title">Assinatura VIP</h2>
                <p className="vipSubtitulo">Sobreviva com vantagens exclusivas dentro do servidor</p>

                {vipAtivo ? (
                    <div className="vipStatusAtual">
                        <span className="vipStatusPulso" />
                        <span>
                            VIP <strong>{user!.vipNivelNome}</strong> ativo até {formatarData(user!.vipExpiraEm!)}
                        </span>
                    </div>
                ) : (
                    <div className="vipStatusAtual vip-status-inativo">
                        <span>Você ainda não tem nenhum plano VIP ativo</span>
                    </div>
                )}
            </div>

            {mensagem && (
                <div className={`vipAviso vipAviso-${mensagem.tipo}`}>
                    {mensagem.texto}
                </div>
            )}

            <div className="grid-tiers">
                {tiers.map(tier => {
                    const ativo = (user?.vipNivel ?? 0) === tier.nivel && vipAtivo;

                    return (
                        <div key={tier.nivel} className={`tier-card ${CLASSE_NIVEL[tier.nivel] ?? ""} ${ativo ? "tier-ativo" : ""}`}>
                            {ativo && <span className="tier-ribbon">Ativo</span>}

                            <div className="tier-emblema">
                                <span className="tier-emblema-icone">★</span>
                            </div>

                            <span className="tier-nome">{tier.nome}</span>
                            <span className="tier-duracao">{tier.duracaoDias} dias de acesso</span>

                            <ul className="tier-beneficios">
                                {(BENEFICIOS[tier.nivel] ?? []).map(beneficio => (
                                    <li key={beneficio}>{beneficio}</li>
                                ))}
                            </ul>

                            <span className="tier-preco">🪙 {tier.preco}</span>

                            <button
                                className="tier-botao"
                                disabled={comprando === tier.nivel}
                                onClick={() => comprar(tier.nivel)}
                            >
                                {comprando === tier.nivel ? "Ativando..." : ativo ? "Renovar" : "Ativar VIP"}
                            </button>
                        </div>
                    );
                })}
            </div>
        </main>
    );
}
