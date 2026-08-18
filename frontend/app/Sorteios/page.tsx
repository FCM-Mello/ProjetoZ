"use client";

import { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import { useToast } from "../contexts/ToastContext";
import { useRequireAuth } from "../hooks/useRequireAuth";
import { useScrollReveal } from "../hooks/useScrollReveal";
import { getSorteios, criarSorteio, entrarSorteio, sortearSorteio, excluirSorteio } from "../services/sorteiosApi";
import { getProducts } from "../services/productsApi";
import { getVipTiers } from "../services/vipApi";
import { Sorteio, CreateSorteioRequest } from "../models/Sorteio";
import { Product } from "../models/Product";
import { VipTier } from "../models/VipTier";
import SorteioModal from "./components/SorteioModal";
import EstadoVazio from "../components/EstadoVazio";
import "./page.css";

export default function Sorteios() {
    useRequireAuth();

    const { user } = useAuth();
    const { sucesso, erro: mostrarErro } = useToast();
    const isAdmin = user?.isAdmin ?? false;

    const [sorteios, setSorteios] = useState<Sorteio[]>([]);
    const [produtos, setProdutos] = useState<Product[]>([]);
    const [vipTiers, setVipTiers] = useState<VipTier[]>([]);
    const [showModal, setShowModal] = useState(false);
    const [processando, setProcessando] = useState<string | null>(null);
    const listaRef = useScrollReveal<HTMLDivElement>(sorteios.length);

    useEffect(() => {
        carregarSorteios();
        getProducts().then(setProdutos).catch(console.error);
        getVipTiers().then(setVipTiers).catch(console.error);
    }, []);

    async function carregarSorteios() {
        try {
            const dados = await getSorteios();
            setSorteios(dados);
        } catch (e) {
            console.error(e);
        }
    }

    async function criarNovoSorteio(request: CreateSorteioRequest) {
        try {
            await criarSorteio(request);
            await carregarSorteios();
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Erro ao criar sorteio.");
        }
    }

    async function entrar(sorteio: Sorteio) {
        setProcessando(sorteio.id);

        try {
            await entrarSorteio(sorteio.id);
            await carregarSorteios();
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Erro ao entrar no sorteio.");
        } finally {
            setProcessando(null);
        }
    }

    async function excluir(sorteio: Sorteio) {
        if (!confirm(`Excluir o sorteio "${sorteio.titulo}"? Essa ação não pode ser desfeita.`))
            return;

        setProcessando(sorteio.id);

        try {
            await excluirSorteio(sorteio.id);
            await carregarSorteios();
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Erro ao excluir sorteio.");
        } finally {
            setProcessando(null);
        }
    }

    async function sortear(sorteio: Sorteio) {
        if (!confirm(`Sortear o vencedor de "${sorteio.titulo}"? Essa ação não pode ser desfeita.`))
            return;

        setProcessando(sorteio.id);

        try {
            const resultado = await sortearSorteio(sorteio.id);
            sucesso(`Vencedor: ${resultado.vencedorNome ?? "usuário"}.`);
            await carregarSorteios();
        } catch (e) {
            console.error(e);
            mostrarErro(e instanceof Error ? e.message : "Erro ao sortear.");
        } finally {
            setProcessando(null);
        }
    }

    return (
        <main className="containerSorteios">
            <div className="sorteiosHeader">
                <h2 className="section-title">Sorteios</h2>

                {isAdmin && (
                    <button className="btnCriarSorteio" onClick={() => setShowModal(true)}>
                        Criar Sorteio
                    </button>
                )}
            </div>

            {sorteios.length === 0 && (
                <EstadoVazio
                    icone="🎟️"
                    titulo="Nenhum sorteio disponível no momento."
                    descricao="Fique de olho — novos sorteios aparecem aqui assim que forem criados."
                />
            )}

            <div className="lista-sorteios" ref={listaRef}>
                {sorteios.map((sorteio, i) => (
                    <div
                        key={sorteio.id}
                        className={`sorteio-card status-${sorteio.status} reveal`}
                        style={{ transitionDelay: `${Math.min(i, 8) * 45}ms` }}
                    >
                        <div className="sorteio-cabecalho">
                            <h3>{sorteio.titulo}</h3>

                            <div className="sorteio-cabecalho-direita">
                                <span className={`sorteio-status status-${sorteio.status}`}>
                                    {sorteio.status === "aberto" ? "Aberto" : "Encerrado"}
                                </span>

                                {isAdmin && (
                                    <button
                                        className="btnExcluirSorteio"
                                        title="Excluir sorteio"
                                        disabled={processando === sorteio.id}
                                        onClick={() => excluir(sorteio)}
                                    >
                                        ✕
                                    </button>
                                )}
                            </div>
                        </div>

                        {sorteio.descricao && (
                            <p className="sorteio-descricao">{sorteio.descricao}</p>
                        )}

                        <div className="sorteio-premios">
                            {sorteio.premioVipNivelNome && (
                                <span className="sorteio-premio premio-vip">
                                    ★ VIP {sorteio.premioVipNivelNome}
                                </span>
                            )}

                            {sorteio.premioProdutos.map(produto => (
                                <span key={produto.id} className="sorteio-premio premio-produto">
                                    {produto.imagem && <img src={produto.imagem} alt={produto.nome} />}
                                    {produto.nome}
                                </span>
                            ))}
                        </div>

                        <div className="sorteio-rodape">
                            <span className="sorteio-participantes">
                                {sorteio.totalParticipantes} {sorteio.totalParticipantes === 1 ? "participante" : "participantes"}
                            </span>

                            {sorteio.status === "sorteado" ? (
                                <span className="sorteio-vencedor">🏆 {sorteio.vencedorNome}</span>
                            ) : (
                                <div className="sorteio-acoes">
                                    {isAdmin && (
                                        <button
                                            className="btnSortear"
                                            disabled={processando === sorteio.id}
                                            onClick={() => sortear(sorteio)}
                                        >
                                            Sortear
                                        </button>
                                    )}

                                    <button
                                        className="btnEntrar"
                                        disabled={sorteio.jaParticipando || processando === sorteio.id}
                                        onClick={() => entrar(sorteio)}
                                    >
                                        {sorteio.jaParticipando ? "Você está participando" : "Entrar"}
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>
                ))}
            </div>

            {showModal && (
                <SorteioModal
                    produtos={produtos}
                    vipTiers={vipTiers}
                    onClose={() => setShowModal(false)}
                    onSave={criarNovoSorteio}
                />
            )}
        </main>
    );
}
