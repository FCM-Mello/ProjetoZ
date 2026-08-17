"use client";

import { useEffect, useRef, useState, type CSSProperties, type MouseEvent } from "react";
import { getCoinPackages, criarCheckoutCoins } from "../services/coinsApi";
import { useAuth } from "../contexts/AuthContext";
import { useToast } from "../contexts/ToastContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { CoinPackage } from "../models/CoinPackage";
import "./page.css";

type StatusRetorno = "success" | "pending" | "failure" | null;

const MENSAGENS_RETORNO: Record<Exclude<StatusRetorno, null>, string> = {
    success: "Pagamento aprovado! Seus Az Coins serão creditados em instantes.",
    pending: "Pagamento em processamento. Assim que for aprovado, os coins são creditados automaticamente.",
    failure: "Não foi possível concluir o pagamento. Nenhum valor foi cobrado.",
};

function reduzMovimento() {
    return typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

// Anima o saldo subindo até o valor atual em vez de trocar seco — dispara de
// novo toda vez que `valor` muda (ex: depois que o polling do webhook credita
// os coins). Sem reduced-motion, pula direto pro valor final.
function useCountUp(valor: number) {
    const [exibido, setExibido] = useState(0);
    const anteriorRef = useRef(0);

    useEffect(() => {
        const de = anteriorRef.current;
        const para = valor;

        if (de === para) return;

        if (reduzMovimento()) {
            setExibido(para);
            anteriorRef.current = para;
            return;
        }

        const duracaoMs = 700;
        const inicio = performance.now();
        let frame: number;

        function passo(agora: number) {
            const p = Math.min((agora - inicio) / duracaoMs, 1);
            const facilitado = 1 - Math.pow(1 - p, 3);
            setExibido(Math.round(de + (para - de) * facilitado));

            if (p < 1) {
                frame = requestAnimationFrame(passo);
            } else {
                anteriorRef.current = para;
            }
        }

        frame = requestAnimationFrame(passo);
        return () => cancelAnimationFrame(frame);
    }, [valor]);

    return exibido;
}

interface ParticulaMoeda {
    id: number;
    pacoteId: string;
    ox: number;
    oy: number;
    dx: number;
    dy: number;
    rot: number;
}

interface ConfetePedaco {
    id: number;
    left: number;
    cor: string;
    dx: number;
    rot: number;
    dur: number;
    delay: number;
}

const CORES_CONFETE = ["#ff9f1c", "#ffd166", "#38bdf8", "#22c55e"];

export default function Az() {
    useRequireAuth();

    const { user, refreshUser } = useAuth();
    const { erro: mostrarErro } = useToast();

    const [pacotes, setPacotes] = useState<CoinPackage[]>([]);
    const [iniciandoCheckout, setIniciandoCheckout] = useState<string | null>(null);
    const [statusRetorno, setStatusRetorno] = useState<StatusRetorno>(null);

    // Partículas/confete precisam ser estado do React, não nós de DOM
    // inseridos manualmente: o cofre e os cards re-renderizam a cada frame
    // durante o count-up do saldo, e o React reconciliaria (removeria) nós
    // que ele não reconhece na própria subárvore no próximo render.
    const [particulasMoeda, setParticulasMoeda] = useState<ParticulaMoeda[]>([]);
    const [confetes, setConfetes] = useState<ConfetePedaco[]>([]);
    const idParticulaRef = useRef(0);
    const idConfeteRef = useRef(0);

    const vaultRef = useRef<HTMLDivElement>(null);
    const saldoExibido = useCountUp(user?.coins ?? 0);
    const gridRef = useScrollReveal<HTMLDivElement>(pacotes.length);

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

    useEffect(() => {
        if (statusRetorno === "success" && vaultRef.current) {
            choverConfete(vaultRef.current.clientWidth);
        }
    }, [statusRetorno]);

    async function carregarPacotes() {
        try {
            const dados = await getCoinPackages();
            setPacotes(dados);
        } catch (e) {
            console.error(e);
        }
    }

    function estourarMoedas(pacoteId: string, card: HTMLElement, origem: HTMLElement) {
        if (reduzMovimento()) return;

        const cardRect = card.getBoundingClientRect();
        const origemRect = origem.getBoundingClientRect();
        const ox = origemRect.left - cardRect.left + origemRect.width / 2;
        const oy = origemRect.top - cardRect.top;

        const novas: ParticulaMoeda[] = [];

        for (let i = 0; i < 8; i++) {
            const angulo = -Math.PI / 2 + (Math.random() - 0.5) * Math.PI * 0.8;
            const dist = 40 + Math.random() * 55;

            novas.push({
                id: idParticulaRef.current++,
                pacoteId,
                ox,
                oy,
                dx: Math.cos(angulo) * dist,
                dy: Math.sin(angulo) * dist,
                rot: Math.random() * 360 - 180,
            });
        }

        setParticulasMoeda(atual => [...atual, ...novas]);
        setTimeout(() => {
            const idsNovas = new Set(novas.map(p => p.id));
            setParticulasMoeda(atual => atual.filter(p => !idsNovas.has(p.id)));
        }, 900);
    }

    function choverConfete(largura: number) {
        if (reduzMovimento()) return;

        const novos: ConfetePedaco[] = [];

        for (let i = 0; i < 26; i++) {
            novos.push({
                id: idConfeteRef.current++,
                left: Math.random() * largura,
                cor: CORES_CONFETE[i % CORES_CONFETE.length],
                dx: Math.random() * 80 - 40,
                rot: 300 + Math.random() * 240,
                dur: 1800 + Math.random() * 900,
                delay: Math.random() * 300,
            });
        }

        setConfetes(atual => [...atual, ...novos]);
        setTimeout(() => {
            const idsNovos = new Set(novos.map(c => c.id));
            setConfetes(atual => atual.filter(c => !idsNovos.has(c.id)));
        }, 3000);
    }

    async function comprar(pacote: CoinPackage, evento: MouseEvent<HTMLButtonElement>) {
        setIniciandoCheckout(pacote.id);

        const card = evento.currentTarget.closest<HTMLElement>(".pacote-card");
        if (card) estourarMoedas(pacote.id, card, evento.currentTarget);

        try {
            const { redirectUrl } = await criarCheckoutCoins(pacote.id);

            // Pequeno atraso só quando a animação de estouro realmente tocou,
            // pra dar tempo dela aparecer antes do redirect descarregar a
            // página (com reduced-motion, `card` continua truthy mas a
            // animação não roda — não vale a pena atrasar nesse caso).
            if (card && !reduzMovimento()) {
                setTimeout(() => { window.location.href = redirectUrl; }, 320);
            } else {
                window.location.href = redirectUrl;
            }
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Não foi possível iniciar o pagamento.");
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
            <div className="azVault" ref={vaultRef}>
                <div className="azVaultGlow" />

                <h2 className="section-title">Az Coins</h2>
                <p className="azSubtitulo">Compre Az Coins e desbloqueie itens exclusivos na loja</p>

                <div className="saldoAtual">
                    <span className="saldoLabel">Saldo atual</span>
                    <span className="saldoValor">🪙 {saldoExibido.toLocaleString("pt-BR")}</span>
                </div>

                {confetes.map(c => (
                    <span
                        key={c.id}
                        className="confetePedaco"
                        style={{
                            left: c.left,
                            background: c.cor,
                            "--dx": `${c.dx}px`,
                            "--rot": `${c.rot}deg`,
                            "--dur": `${c.dur}ms`,
                            "--delay": `${c.delay}ms`,
                        } as CSSProperties}
                    />
                ))}
            </div>

            {statusRetorno && (
                <div className={`avisoRetorno aviso-${statusRetorno}`}>
                    {MENSAGENS_RETORNO[statusRetorno]}
                </div>
            )}

            <div className="grid-pacotes" ref={gridRef}>
                {pacotes.map((pacote, i) => (
                    <div
                        key={pacote.id}
                        className={`pacote-card pacote-${pacote.id} reveal`}
                        style={{ transitionDelay: `${i * 60}ms` }}
                    >
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
                            onClick={(e) => comprar(pacote, e)}
                        >
                            {iniciandoCheckout === pacote.id ? "Redirecionando..." : "Comprar"}
                        </button>

                        {particulasMoeda
                            .filter(p => p.pacoteId === pacote.id)
                            .map(p => (
                                <span
                                    key={p.id}
                                    className="coinParticula"
                                    style={{
                                        left: p.ox,
                                        top: p.oy,
                                        "--dx": `${p.dx}px`,
                                        "--dy": `${p.dy}px`,
                                        "--rot": `${p.rot}deg`,
                                    } as CSSProperties}
                                >
                                    🪙
                                </span>
                            ))}
                    </div>
                ))}
            </div>
        </main>
    );
}
