"use client";

import { useEffect, useMemo, useState } from "react";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useToast } from "../contexts/ToastContext";
import { ClaResumo, ClaDetalhe, ClaBuscaJogador, CriarClaRequest } from "../models/Cla";
import {
    getClas, getMeuCla, criarCla, solicitarEntrada,
    aprovarSolicitacao, removerSolicitacao, promoverAdmin, removerAdmin,
    removerMembro, sairDoCla, desfazerCla, buscarJogadorParaConvidar, convidarParaCla,
} from "../services/clasApi";
import EstadoVazio from "../components/EstadoVazio";
import Skeleton from "../components/Skeleton";
import ClaCriarModal from "./components/ClaCriarModal";
import "./page.css";

type AbaRankingCla = "kd" | "koth" | "zumbis" | "tempo";

const ABAS_RANKING_CLA: { id: AbaRankingCla; label: string; icone: string }[] = [
    { id: "kd", label: "K/D", icone: "⚔️" },
    { id: "koth", label: "KOTH", icone: "🏆" },
    { id: "zumbis", label: "Zumbis", icone: "🧟" },
    { id: "tempo", label: "Tempo", icone: "⏱️" },
];

function formatarTempoCla(segundos: number) {
    const horas = Math.floor(segundos / 3600);
    const minutos = Math.floor((segundos % 3600) / 60);

    return horas > 0 ? `${horas}h ${minutos}min` : `${minutos}min`;
}

export default function Cla() {
    useRequireAuth();

    const { sucesso, erro: mostrarErro } = useToast();

    const [carregando, setCarregando] = useState(true);
    const [meuCla, setMeuCla] = useState<ClaDetalhe | null>(null);
    const [lista, setLista] = useState<ClaResumo[]>([]);
    const [solicitados, setSolicitados] = useState<Set<string>>(new Set());
    const [showCriarModal, setShowCriarModal] = useState(false);
    const [processando, setProcessando] = useState<string | null>(null);

    const [buscaConvite, setBuscaConvite] = useState("");
    const [resultadosConvite, setResultadosConvite] = useState<ClaBuscaJogador[]>([]);
    const [buscandoConvite, setBuscandoConvite] = useState(false);
    const [convidados, setConvidados] = useState<Set<string>>(new Set());

    const [abaRanking, setAbaRanking] = useState<AbaRankingCla>("kd");

    const membrosRankeados = useMemo(() => {
        if (!meuCla) return [];
        const copia = [...meuCla.membros];

        switch (abaRanking) {
            case "koth":
                return copia.sort((a, b) => b.kothCompletados - a.kothCompletados || b.kd - a.kd);
            case "zumbis":
                return copia.sort((a, b) => b.zumbiKills - a.zumbiKills || b.kd - a.kd);
            case "tempo":
                return copia.sort((a, b) => b.segundosJogados - a.segundosJogados || b.kd - a.kd);
            default:
                return copia.sort((a, b) => b.kd - a.kd || b.kothCompletados - a.kothCompletados);
        }
    }, [meuCla, abaRanking]);

    useEffect(() => {
        carregar();
    }, []);

    useEffect(() => {
        if (!meuCla?.souAdmin || buscaConvite.trim().length < 2) {
            setResultadosConvite([]);
            return;
        }

        const timeout = setTimeout(async () => {
            setBuscandoConvite(true);
            try {
                setResultadosConvite(await buscarJogadorParaConvidar(meuCla.id, buscaConvite.trim()));
            } catch (e) {
                console.error(e);
            } finally {
                setBuscandoConvite(false);
            }
        }, 350);

        return () => clearTimeout(timeout);
    }, [buscaConvite, meuCla?.id, meuCla?.souAdmin]);

    async function carregar() {
        setCarregando(true);

        try {
            const cla = await getMeuCla();
            setMeuCla(cla);

            if (!cla) {
                const clas = await getClas();
                setLista(clas);
            }
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Erro ao carregar clãs.");
        } finally {
            setCarregando(false);
        }
    }

    async function handleCriar(request: CriarClaRequest) {
        try {
            await criarCla(request);
            setShowCriarModal(false);
            sucesso("Clã criado!");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao criar clã.");
        }
    }

    async function handleSolicitar(claId: string) {
        setProcessando(claId);

        try {
            await solicitarEntrada(claId);
            setSolicitados(atual => new Set(atual).add(claId));
            sucesso("Solicitação enviada!");
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao solicitar entrada.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleAprovar(solicitacaoId: string) {
        if (!meuCla) return;
        setProcessando(solicitacaoId);

        try {
            await aprovarSolicitacao(meuCla.id, solicitacaoId);
            sucesso("Membro aprovado!");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao aprovar solicitação.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleRejeitar(solicitacaoId: string) {
        if (!meuCla) return;
        setProcessando(solicitacaoId);

        try {
            await removerSolicitacao(meuCla.id, solicitacaoId);
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao rejeitar solicitação.");
        } finally {
            setProcessando(null);
        }
    }

    async function handlePromover(userId: string) {
        if (!meuCla) return;
        setProcessando(userId);

        try {
            await promoverAdmin(meuCla.id, userId);
            sucesso("Membro promovido a admin.");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao promover membro.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleRemoverAdmin(userId: string) {
        if (!meuCla) return;
        setProcessando(userId);

        try {
            await removerAdmin(meuCla.id, userId);
            sucesso("Admin removido.");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao remover admin.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleRemoverMembro(userId: string, nome: string) {
        if (!meuCla) return;
        if (!confirm(`Remover ${nome} do clã?`)) return;

        setProcessando(userId);

        try {
            await removerMembro(meuCla.id, userId);
            sucesso(`${nome} foi removido do clã.`);
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao remover membro.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleConvidar(jogador: ClaBuscaJogador) {
        if (!meuCla) return;
        setProcessando(jogador.userId);

        try {
            await convidarParaCla(meuCla.id, jogador.userId);
            setConvidados(atual => new Set(atual).add(jogador.userId));
            sucesso(`Convite enviado pra ${jogador.nome}.`);
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao convidar jogador.");
        } finally {
            setProcessando(null);
        }
    }

    async function handleSair() {
        if (!meuCla) return;
        if (!confirm(`Sair do clã "${meuCla.nome}"?`)) return;

        try {
            await sairDoCla(meuCla.id);
            sucesso("Você saiu do clã.");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao sair do clã.");
        }
    }

    async function handleDesfazer() {
        if (!meuCla) return;
        if (!confirm(`Desfazer o clã "${meuCla.nome}"? Essa ação não pode ser desfeita.`)) return;

        try {
            await desfazerCla(meuCla.id);
            sucesso("Clã desfeito.");
            await carregar();
        } catch (e) {
            mostrarErro(e instanceof Error ? e.message : "Erro ao desfazer clã.");
        }
    }

    if (carregando) {
        return (
            <main className="containerCla">
                <h2 className="section-title">Clã</h2>
                <div className="claListaBrowse">
                    {[1, 2, 3].map(i => <Skeleton key={i} height={72} borderRadius={10} />)}
                </div>
            </main>
        );
    }

    return (
        <main className="containerCla">
            <div className="claHeader">
                <h2 className="section-title">Clã</h2>
            </div>

            {meuCla ? (
                <div className="claPainel">
                    <div className="claBanner">
                        {meuCla.estandarte ? (
                            <img src={meuCla.estandarte} alt={meuCla.nome} className="claEstandarte" />
                        ) : (
                            <div className="claEstandarte claEstandarte-vazio">🛡️</div>
                        )}

                        <div className="claBannerInfo">
                            <h3>{meuCla.nome}</h3>
                            <div className="claBannerRule" />
                            {meuCla.descricao && <p>{meuCla.descricao}</p>}
                            <span className="claTotalMembros">{meuCla.membros.length} {meuCla.membros.length === 1 ? "membro" : "membros"}</span>
                        </div>

                        <div className="claBannerSelos">
                            <div className="claBannerSelo">
                                <span className="claBannerSeloValor">{meuCla.estatisticas.kdMedio.toFixed(2)}</span>
                                <span className="claBannerSeloLabel">K/D</span>
                            </div>
                            <div className="claBannerSelo">
                                <span className="claBannerSeloValor">{meuCla.estatisticas.totalKothCompletados}</span>
                                <span className="claBannerSeloLabel">KOTH</span>
                            </div>
                            <div className="claBannerSelo">
                                <span className="claBannerSeloValor">{meuCla.estatisticas.totalZumbiKills}</span>
                                <span className="claBannerSeloLabel">Zumbis</span>
                            </div>
                        </div>
                    </div>

                    <section className="claSecao">
                        <h4>Estatísticas do Clã</h4>
                        <div className="claStatsGrid">
                            <div className="claStatTile">
                                <span className="claStatIcone">⚔️</span>
                                <span className="claStatValor">{meuCla.estatisticas.kdMedio.toFixed(2)}</span>
                                <span className="claStatLabel">K/D médio</span>
                            </div>
                            <div className="claStatTile">
                                <span className="claStatIcone">💀</span>
                                <span className="claStatValor">{meuCla.estatisticas.totalKills}</span>
                                <span className="claStatLabel">Kills</span>
                            </div>
                            <div className="claStatTile">
                                <span className="claStatIcone">☠️</span>
                                <span className="claStatValor">{meuCla.estatisticas.totalDeaths}</span>
                                <span className="claStatLabel">Mortes</span>
                            </div>
                            <div className="claStatTile">
                                <span className="claStatIcone">🏆</span>
                                <span className="claStatValor">{meuCla.estatisticas.totalKothCompletados}</span>
                                <span className="claStatLabel">KOTH completados</span>
                            </div>
                            <div className="claStatTile">
                                <span className="claStatIcone">🧟</span>
                                <span className="claStatValor">{meuCla.estatisticas.totalZumbiKills}</span>
                                <span className="claStatLabel">Zumbis mortos</span>
                            </div>
                            <div className="claStatTile">
                                <span className="claStatIcone">⏱️</span>
                                <span className="claStatValor">{formatarTempoCla(meuCla.estatisticas.totalSegundosJogados)}</span>
                                <span className="claStatLabel">Tempo de sobrevivência</span>
                            </div>
                        </div>
                    </section>

                    <section className="claSecao">
                        <h4>Ranking do Clã</h4>
                        <nav className="claRankingAbas">
                            {ABAS_RANKING_CLA.map(a => (
                                <button
                                    key={a.id}
                                    className={`claRankingAba ${abaRanking === a.id ? "claRankingAba-ativa" : ""}`}
                                    onClick={() => setAbaRanking(a.id)}
                                >
                                    {a.icone} {a.label}
                                </button>
                            ))}
                        </nav>

                        <div className="claRankingLista">
                            {membrosRankeados.map((m, i) => (
                                <div key={m.userId ?? `${m.nome}-${i}`} className="claRankingItem">
                                    <span className="claRankingPosicao">{i + 1}º</span>
                                    <img src={m.avatar} alt={m.nome} className="claAvatar" />
                                    <span className="claNomeItem">{m.nome}</span>
                                    {m.isLider && <span className="claSelo">Líder</span>}
                                    {!m.isLider && m.isAdmin && <span className="claSelo claSelo-admin">Admin</span>}

                                    <div className="claRankingStats">
                                        <span className={`claRankingStatDestaque ${abaRanking === "kd" ? "claRankingStatDestaque-ativa" : ""}`}>
                                            K/D {m.kd.toFixed(2)}
                                        </span>
                                        <span className={`claRankingStatDestaque ${abaRanking === "koth" ? "claRankingStatDestaque-ativa" : ""}`}>
                                            🏆 {m.kothCompletados}
                                        </span>
                                        <span className={`claRankingStatDestaque ${abaRanking === "zumbis" ? "claRankingStatDestaque-ativa" : ""}`}>
                                            🧟 {m.zumbiKills}
                                        </span>
                                        <span className={`claRankingStatDestaque ${abaRanking === "tempo" ? "claRankingStatDestaque-ativa" : ""}`}>
                                            ⏱️ {formatarTempoCla(m.segundosJogados)}
                                        </span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </section>

                    {meuCla.souAdmin && meuCla.solicitacoes.length > 0 && (
                        <section className="claSecao">
                            <h4>Solicitações pendentes</h4>
                            <ul className="claListaSolicitacoes">
                                {meuCla.solicitacoes.map(s => (
                                    <li key={s.id} className="claSolicitacaoItem">
                                        <img src={s.avatar} alt={s.nome} className="claAvatar" />
                                        <span className="claNomeItem">{s.nome}</span>

                                        <div className="claAcoes">
                                            <button
                                                className="btnClaAprovar"
                                                disabled={processando === s.id}
                                                onClick={() => handleAprovar(s.id)}
                                            >
                                                Aprovar
                                            </button>
                                            <button
                                                className="btnClaRejeitar"
                                                disabled={processando === s.id}
                                                onClick={() => handleRejeitar(s.id)}
                                            >
                                                Rejeitar
                                            </button>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </section>
                    )}

                    <section className="claSecao">
                        <h4>Membros</h4>
                        <ul className="claListaMembros">
                            {meuCla.membros.map(m => (
                                <li key={m.userId} className="claMembroItem">
                                    <img src={m.avatar} alt={m.nome} className="claAvatar" />
                                    <span className="claNomeItem">{m.nome}</span>

                                    {m.isLider && <span className="claSelo">Líder</span>}
                                    {!m.isLider && m.isAdmin && <span className="claSelo claSelo-admin">Admin</span>}

                                    {!m.isLider && meuCla.souAdmin && m.userId && (
                                        <div className="claAcoes">
                                            {!m.isAdmin && (
                                                <button
                                                    className="btnClaSecundario"
                                                    disabled={processando === m.userId}
                                                    onClick={() => handlePromover(m.userId!)}
                                                >
                                                    Promover a admin
                                                </button>
                                            )}

                                            {m.isAdmin && meuCla.souLider && (
                                                <button
                                                    className="btnClaSecundario"
                                                    disabled={processando === m.userId}
                                                    onClick={() => handleRemoverAdmin(m.userId!)}
                                                >
                                                    Remover admin
                                                </button>
                                            )}

                                            {meuCla.souLider && (
                                                <button
                                                    className="btnClaSecundarioPerigo"
                                                    disabled={processando === m.userId}
                                                    onClick={() => handleRemoverMembro(m.userId!, m.nome)}
                                                >
                                                    Remover do clã
                                                </button>
                                            )}
                                        </div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    </section>

                    {meuCla.souAdmin && (
                        <section className="claSecao">
                            <h4>Convidar jogador</h4>
                            <input
                                className="claBuscaInput"
                                placeholder="Buscar por nome ou SteamID..."
                                value={buscaConvite}
                                onChange={(e) => setBuscaConvite(e.target.value)}
                            />

                            {buscandoConvite && <p className="claBuscaStatus">Buscando...</p>}

                            {!buscandoConvite && buscaConvite.trim().length >= 2 && resultadosConvite.length === 0 && (
                                <p className="claBuscaStatus">Nenhum jogador encontrado.</p>
                            )}

                            {resultadosConvite.length > 0 && (
                                <ul className="claListaMembros">
                                    {resultadosConvite.map(jogador => (
                                        <li key={jogador.userId} className="claMembroItem">
                                            <img src={jogador.avatar} alt={jogador.nome} className="claAvatar" />
                                            <span className="claNomeItem">{jogador.nome}</span>

                                            <button
                                                className="btnClaSecundario"
                                                disabled={processando === jogador.userId || convidados.has(jogador.userId)}
                                                onClick={() => handleConvidar(jogador)}
                                            >
                                                {convidados.has(jogador.userId) ? "Convite enviado" : "Convidar"}
                                            </button>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>
                    )}

                    <div className="claAcoesFinais">
                        {meuCla.souLider ? (
                            <button className="btnClaPerigo" onClick={handleDesfazer}>Desfazer Clã</button>
                        ) : (
                            <button className="btnClaPerigo" onClick={handleSair}>Sair do Clã</button>
                        )}
                    </div>
                </div>
            ) : (
                <>
                    <div className="claListaHeader">
                        <p className="claListaSubtitulo">Você ainda não faz parte de um clã.</p>
                        <button className="btnClaCriar" onClick={() => setShowCriarModal(true)}>Criar Clã</button>
                    </div>

                    {lista.length === 0 ? (
                        <EstadoVazio
                            icone="🛡️"
                            titulo="Nenhum clã criado ainda."
                            descricao="Seja o primeiro a criar um."
                        />
                    ) : (
                        <ul className="claListaBrowse">
                            {lista.map(c => (
                                <li key={c.id} className="claLinha">
                                    <div className="claLinhaEmblemaCaixa">
                                        {c.estandarte ? (
                                            <img src={c.estandarte} alt={c.nome} className="claLinhaEmblema" />
                                        ) : (
                                            <div className="claLinhaEmblema claLinhaEmblema-vazio">
                                                {c.nome.slice(0, 2).toUpperCase()}
                                            </div>
                                        )}
                                    </div>

                                    <div className="claLinhaInfo">
                                        <span className="claLinhaNome">{c.nome}</span>
                                        {c.descricao && <p className="claLinhaDescricao">{c.descricao}</p>}
                                        <span className="claLinhaLider">Líder: {c.liderNome}</span>
                                    </div>

                                    <span className="claLinhaMembros">
                                        {c.totalMembros} {c.totalMembros === 1 ? "membro" : "membros"}
                                    </span>

                                    <button
                                        className="btnClaSolicitar"
                                        disabled={processando === c.id || solicitados.has(c.id)}
                                        onClick={() => handleSolicitar(c.id)}
                                    >
                                        {solicitados.has(c.id) ? "Solicitação enviada" : "Solicitar entrada"}
                                    </button>
                                </li>
                            ))}
                        </ul>
                    )}
                </>
            )}

            {showCriarModal && (
                <ClaCriarModal
                    onClose={() => setShowCriarModal(false)}
                    onSave={handleCriar}
                />
            )}
        </main>
    );
}
