"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRequireAdmin } from "../hooks/useRequireAdmin";
import { getUsuarios } from "../services/adminApi";
import { getVipTiers } from "../services/vipApi";
import { getProducts } from "../services/productsApi";
import { AdminUsuario } from "../models/AdminUsuario";
import { VipTier } from "../models/VipTier";
import { Product } from "../models/Product";
import UsuarioAdminModal from "./components/UsuarioAdminModal";
import "./page.css";

export default function Admin() {
    useRequireAdmin();

    const [usuarios, setUsuarios] = useState<AdminUsuario[]>([]);
    const [vipTiers, setVipTiers] = useState<VipTier[]>([]);
    const [produtos, setProdutos] = useState<Product[]>([]);
    const [busca, setBusca] = useState("");
    const [carregando, setCarregando] = useState(true);
    const [usuarioSelecionado, setUsuarioSelecionado] = useState<string | null>(null);

    useEffect(() => {
        carregarUsuarios();
        getVipTiers().then(setVipTiers).catch(console.error);
        getProducts().then(setProdutos).catch(console.error);
    }, []);

    useEffect(() => {
        const timeout = setTimeout(carregarUsuarios, 300);
        return () => clearTimeout(timeout);
    }, [busca]);

    async function carregarUsuarios() {
        setCarregando(true);
        try {
            const dados = await getUsuarios(busca || undefined);
            setUsuarios(dados);
        } catch (e) {
            console.error(e);
        } finally {
            setCarregando(false);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleDateString("pt-BR");
    }

    return (
        <main className="containerAdmin">
            <div className="adminHeader">
                <h2 className="section-title">Administração</h2>

                <Link href="/Admin/Notificacoes" className="adminLinkNotificacoes">Notificações</Link>

                <input
                    className="adminBusca"
                    placeholder="Buscar por nome ou SteamID..."
                    value={busca}
                    onChange={(e) => setBusca(e.target.value)}
                />
            </div>

            {!carregando && usuarios.length === 0 && (
                <p className="adminVazio">Nenhum usuário encontrado.</p>
            )}

            <div className="lista-usuarios-admin">
                {usuarios.map(usuario => (
                    <div
                        key={usuario.id}
                        className="usuario-admin-card"
                        onClick={() => setUsuarioSelecionado(usuario.id)}
                    >
                        {usuario.avatar && <img src={usuario.avatar} alt={usuario.nome} />}

                        <div className="usuario-admin-info">
                            <span className="usuario-admin-nome">{usuario.nome}</span>
                            <span className="usuario-admin-steamid">{usuario.steamId}</span>
                        </div>

                        <div className="usuario-admin-badges">
                            <span className="badge-coins">🪙 {usuario.coins}</span>

                            {usuario.vipNivel > 0 && (
                                <span className="badge-vip">★ {usuario.vipNivelNome} até {formatarData(usuario.vipExpiraEm!)}</span>
                            )}

                            {usuario.isAdmin && (
                                <span className="badge-admin">Admin</span>
                            )}

                            {usuario.banido && (
                                <span className="badge-banido">Banido</span>
                            )}
                        </div>
                    </div>
                ))}
            </div>

            {usuarioSelecionado && (
                <UsuarioAdminModal
                    usuarioId={usuarioSelecionado}
                    vipTiers={vipTiers}
                    produtos={produtos}
                    onClose={() => setUsuarioSelecionado(null)}
                    onChange={carregarUsuarios}
                />
            )}
        </main>
    );
}
