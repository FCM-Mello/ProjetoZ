"use client";

import { useEffect, useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import {
    getUsuario,
    ajustarCoins,
    zerarCoins,
    definirVip,
    removerVip,
    adicionarProduto,
    removerProduto,
    tornarAdmin,
    removerAdmin,
} from "../../services/adminApi";
import { AdminUsuarioDetalhe } from "../../models/AdminUsuario";
import { VipTier } from "../../models/VipTier";
import { Product } from "../../models/Product";
import "../../components/Modal.css";
import "./UsuarioAdminModal.css";

// Dono do site — nunca pode perder o admin por aqui.
const SUPER_ADMIN_STEAM_ID = "76561198886359962";

interface Props {
    usuarioId: string;
    vipTiers: VipTier[];
    produtos: Product[];
    onClose: () => void;
    onChange: () => void;
}

export default function UsuarioAdminModal({ usuarioId, vipTiers, produtos, onClose, onChange }: Props) {
    const { user: meuUsuario } = useAuth();

    const [usuario, setUsuario] = useState<AdminUsuarioDetalhe | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [processando, setProcessando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);

    const [valorCoins, setValorCoins] = useState(0);
    const [nivelSelecionado, setNivelSelecionado] = useState<number>(vipTiers[0]?.nivel ?? 1);
    const [produtoSelecionado, setProdutoSelecionado] = useState<string>(produtos[0]?.id ?? "");

    const souEu = meuUsuario?.id === usuarioId;
    const protegido = souEu || usuario?.steamId === SUPER_ADMIN_STEAM_ID;

    useEffect(() => {
        carregar();
    }, [usuarioId]);

    async function carregar() {
        setCarregando(true);
        try {
            const dados = await getUsuario(usuarioId);
            setUsuario(dados);
        } catch (e) {
            console.error(e);
            setErro("Não foi possível carregar o usuário.");
        } finally {
            setCarregando(false);
        }
    }

    async function executar(acao: () => Promise<unknown>) {
        setProcessando(true);
        setErro(null);

        try {
            await acao();
            await carregar();
            onChange();
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Erro ao executar ação.");
        } finally {
            setProcessando(false);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR");
    }

    return (
        <div className="modal-overlay">
            <div className="modal admin-usuario-modal">
                {carregando || !usuario ? (
                    <p className="admin-modal-carregando">Carregando...</p>
                ) : (
                    <>
                        <div className="admin-modal-header">
                            {usuario.avatar && <img src={usuario.avatar} alt={usuario.nome} className="admin-modal-avatar" />}

                            <div>
                                <h2 className="admin-modal-nome">{usuario.nome}</h2>
                                <span className="admin-modal-steamid">{usuario.steamId}</span>
                            </div>
                        </div>

                        {erro && <div className="admin-modal-erro">{erro}</div>}

                        <div className="admin-secao">
                            <h3>Az Coins</h3>
                            <p className="admin-valor-atual">🪙 {usuario.coins}</p>

                            <div className="admin-linha">
                                <input
                                    type="number"
                                    min={1}
                                    value={valorCoins}
                                    onChange={(e) => setValorCoins(Number(e.target.value))}
                                />
                                <button
                                    disabled={processando || valorCoins <= 0}
                                    onClick={() => executar(() => ajustarCoins(usuario.id, valorCoins))}
                                >
                                    Dar
                                </button>
                                <button
                                    disabled={processando || valorCoins <= 0}
                                    onClick={() => executar(() => ajustarCoins(usuario.id, -valorCoins))}
                                >
                                    Remover
                                </button>
                                <button
                                    className="admin-btn-perigo"
                                    disabled={processando}
                                    onClick={() => executar(() => zerarCoins(usuario.id))}
                                >
                                    Zerar
                                </button>
                            </div>
                        </div>

                        <div className="admin-secao">
                            <h3>VIP</h3>
                            <p className="admin-valor-atual">
                                {usuario.vipNivel > 0
                                    ? `${usuario.vipNivelNome} — válido até ${formatarData(usuario.vipExpiraEm!)}`
                                    : "Nenhum"}
                            </p>

                            <div className="admin-linha">
                                <select
                                    value={nivelSelecionado}
                                    onChange={(e) => setNivelSelecionado(Number(e.target.value))}
                                >
                                    {vipTiers.map(tier => (
                                        <option key={tier.nivel} value={tier.nivel}>{tier.nome}</option>
                                    ))}
                                </select>
                                <button
                                    disabled={processando}
                                    onClick={() => executar(() => definirVip(usuario.id, nivelSelecionado))}
                                >
                                    Dar VIP
                                </button>
                                <button
                                    className="admin-btn-perigo"
                                    disabled={processando || usuario.vipNivel === 0}
                                    onClick={() => executar(() => removerVip(usuario.id))}
                                >
                                    Remover VIP
                                </button>
                            </div>
                        </div>

                        <div className="admin-secao">
                            <h3>Inventário</h3>

                            {usuario.inventario.length === 0 ? (
                                <p className="admin-lista-vazia">Nenhum item.</p>
                            ) : (
                                <ul className="admin-lista-inventario">
                                    {usuario.inventario.map(item => (
                                        <li key={item.produtoId}>
                                            <span>{item.nome} {item.quantidade > 1 && `x${item.quantidade}`}</span>
                                            <button
                                                className="admin-btn-perigo"
                                                disabled={processando}
                                                onClick={() => executar(() => removerProduto(usuario.id, item.produtoId))}
                                            >
                                                Remover
                                            </button>
                                        </li>
                                    ))}
                                </ul>
                            )}

                            <div className="admin-linha">
                                <select
                                    value={produtoSelecionado}
                                    onChange={(e) => setProdutoSelecionado(e.target.value)}
                                >
                                    {produtos.map(produto => (
                                        <option key={produto.id} value={produto.id}>{produto.nome}</option>
                                    ))}
                                </select>
                                <button
                                    disabled={processando || !produtoSelecionado}
                                    onClick={() => executar(() => adicionarProduto(usuario.id, produtoSelecionado))}
                                >
                                    Adicionar
                                </button>
                            </div>
                        </div>

                        <div className="admin-secao">
                            <h3>Acesso de admin</h3>
                            <p className="admin-valor-atual">{usuario.isAdmin ? "É admin" : "Não é admin"}</p>

                            <div className="admin-linha">
                                {usuario.isAdmin ? (
                                    <button
                                        className="admin-btn-perigo"
                                        disabled={processando || protegido}
                                        title={protegido ? "Esse acesso de admin não pode ser removido" : undefined}
                                        onClick={() => executar(() => removerAdmin(usuario.id))}
                                    >
                                        Remover admin
                                    </button>
                                ) : (
                                    <button
                                        disabled={processando}
                                        onClick={() => executar(() => tornarAdmin(usuario.id))}
                                    >
                                        Tornar admin
                                    </button>
                                )}
                            </div>
                        </div>
                    </>
                )}

                <div className="modal-buttons">
                    <button className="btnCancel" onClick={onClose}>Fechar</button>
                </div>
            </div>
        </div>
    );
}
