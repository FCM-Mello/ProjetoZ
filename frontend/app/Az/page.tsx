"use client";

import { useEffect, useState } from "react";
import { getCoinPackages, criarCheckoutCoins } from "../services/coinsApi";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { CoinPackage } from "../models/CoinPackage";
import "./page.css";

type StatusRetorno = "success" | "pending" | "failure" | null;

const MENSAGENS_RETORNO: Record<Exclude<StatusRetorno, null>, string> = {
    success: "Pagamento aprovado! Seus Az Coins serão creditados em instantes.",
    pending: "Pagamento em processamento. Assim que for aprovado, os coins são creditados automaticamente.",
    failure: "Não foi possível concluir o pagamento. Nenhum valor foi cobrado.",
};

export default function Az() {
    useRequireAuth();

    const { user, refreshUser } = useAuth();

    const [pacotes, setPacotes] = useState<CoinPackage[]>([]);
    const [iniciandoCheckout, setIniciandoCheckout] = useState<string | null>(null);
    const [statusRetorno, setStatusRetorno] = useState<StatusRetorno>(null);

    useEffect(() => {
        carregarPacotes();

        const status = new URLSearchParams(window.location.search).get("status");

        if (status === "success" || status === "pending" || status === "failure") {
            setStatusRetorno(status);

            // Remove o parâmetro da URL para não reexibir a mensagem em um refresh.
            window.history.replaceState(null, "", "/Az");
        }
    }, []);

    useEffect(() => {
        if (statusRetorno !== "success") return;

        // O crédito dos coins depende do webhook do Mercado Pago, que pode
        // levar alguns segundos para chegar. Enquanto isso, tenta atualizar
        // o saldo algumas vezes.
        let tentativas = 0;

        const intervalo = setInterval(() => {
            tentativas += 1;
            refreshUser();

            if (tentativas >= 5) clearInterval(intervalo);
        }, 2000);

        return () => clearInterval(intervalo);
    }, [statusRetorno]);

    async function carregarPacotes() {
        try {
            const dados = await getCoinPackages();
            setPacotes(dados);
        } catch (e) {
            console.error(e);
        }
    }

    async function comprar(pacote: CoinPackage) {
        setIniciandoCheckout(pacote.id);

        try {
            const { redirectUrl } = await criarCheckoutCoins(pacote.id);
            window.location.href = redirectUrl;
        } catch (e) {
            console.error(e);
            alert(e instanceof Error ? e.message : "Não foi possível iniciar o pagamento.");
            setIniciandoCheckout(null);
        }
    }

    const melhorValor = pacotes.length > 0
        ? pacotes.reduce((melhor, atual) =>
            (atual.coins / atual.precoReais) > (melhor.coins / melhor.precoReais) ? atual : melhor
        ).id
        : null;

    return (
        <main className="containerAz">
            <div className="azVault">
                <div className="azVaultGlow" />

                <h2 className="section-title">Az Coins</h2>
                <p className="azSubtitulo">Compre Az Coins e desbloqueie itens exclusivos na loja</p>

                <div className="saldoAtual">
                    <span className="saldoLabel">Saldo atual</span>
                    <span className="saldoValor">🪙 {user?.coins ?? 0}</span>
                </div>
            </div>

            {statusRetorno && (
                <div className={`avisoRetorno aviso-${statusRetorno}`}>
                    {MENSAGENS_RETORNO[statusRetorno]}
                </div>
            )}

            <div className="grid-pacotes">
                {pacotes.map(pacote => (
                    <div key={pacote.id} className={`pacote-card pacote-${pacote.id}`}>
                        {pacote.id === melhorValor && (
                            <span className="pacote-badge">Melhor valor</span>
                        )}

                        <div className="pacote-medalha">
                            <span className="pacote-medalha-icone">🪙</span>
                        </div>

                        <span className="pacote-nome">{pacote.nome}</span>

                        <span className="pacote-coins">{pacote.coins.toLocaleString("pt-BR")}</span>

                        <span className="pacote-preco">
                            R$ {pacote.precoReais.toFixed(2)}
                        </span>

                        <button
                            disabled={iniciandoCheckout === pacote.id}
                            onClick={() => comprar(pacote)}
                        >
                            {iniciandoCheckout === pacote.id ? "Redirecionando..." : "Comprar"}
                        </button>
                    </div>
                ))}
            </div>
        </main>
    );
}
