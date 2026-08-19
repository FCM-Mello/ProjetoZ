"use client";

import { useEffect, useState } from "react";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useToast } from "../contexts/ToastContext";
import { ClaResumo, ClaDetalhe, CriarClaRequest } from "../models/Cla";
import {
    getClas, getMeuCla, criarCla, solicitarEntrada,
    aprovarSolicitacao, removerSolicitacao, promoverAdmin, removerAdmin,
    sairDoCla, desfazerCla,
} from "../services/clasApi";
import Badge from "../components/Badge";
import EstadoVazio from "../components/EstadoVazio";
import Skeleton from "../components/Skeleton";
import ClaCriarModal from "./components/ClaCriarModal";
import "./page.css";

export default function Cla() {
    useRequireAuth();

    const { sucesso, erro: mostrarErro } = useToast();

    const [carregando, setCarregando] = useState(true);
    const [meuCla, setMeuCla] = useState<ClaDetalhe | null>(null);
    const [lista, setLista] = useState<ClaResumo[]>([]);
    const [solicitados, setSolicitados] = useState<Set<string>>(new Set());
    const [showCriarModal, setShowCriarModal] = useState(false);
    const [processando, setProcessando] = useState<string | null>(null);

    useEffect(() => {
        carregar();
    }, []);

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
                <div className="claGridLista">
                    {[1, 2, 3].map(i => <Skeleton key={i} height={180} borderRadius={12} />)}
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
                            {meuCla.descricao && <p>{meuCla.descricao}</p>}
                            <span className="claTotalMembros">{meuCla.membros.length} {meuCla.membros.length === 1 ? "membro" : "membros"}</span>
                        </div>
                    </div>

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

                                    {m.isLider && <Badge tom="accent">Líder</Badge>}
                                    {!m.isLider && m.isAdmin && <Badge tom="info">Admin</Badge>}

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
                                        </div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    </section>

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
                        <div className="claGridLista">
                            {lista.map(c => (
                                <div key={c.id} className="claCard">
                                    {c.estandarte ? (
                                        <img src={c.estandarte} alt={c.nome} className="claCardEstandarte" />
                                    ) : (
                                        <div className="claCardEstandarte claEstandarte-vazio">🛡️</div>
                                    )}

                                    <span className="claCardNome">{c.nome}</span>
                                    {c.descricao && <p className="claCardDescricao">{c.descricao}</p>}

                                    <div className="claCardMeta">
                                        <span>{c.totalMembros} {c.totalMembros === 1 ? "membro" : "membros"}</span>
                                        <span>Líder: {c.liderNome}</span>
                                    </div>

                                    <button
                                        className="btnClaSolicitar"
                                        disabled={processando === c.id || solicitados.has(c.id)}
                                        onClick={() => handleSolicitar(c.id)}
                                    >
                                        {solicitados.has(c.id) ? "Solicitação enviada" : "Solicitar entrada"}
                                    </button>
                                </div>
                            ))}
                        </div>
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
