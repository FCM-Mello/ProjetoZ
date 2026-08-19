"use client";

import { useEffect, useState } from "react";
import { getClasAdmin } from "../../services/adminApi";
import { AdminCla } from "../../models/AdminCla";
import ClaAdminModal from "../components/ClaAdminModal";
import EstadoVazio from "../../components/EstadoVazio";
import "../page.css";
import "./page.css";

export default function AdminClas() {
    const [clas, setClas] = useState<AdminCla[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [claSelecionado, setClaSelecionado] = useState<string | null>(null);

    useEffect(() => {
        carregar();
    }, []);

    async function carregar() {
        setCarregando(true);
        try {
            setClas(await getClasAdmin());
        } catch (e) {
            console.error(e);
        } finally {
            setCarregando(false);
        }
    }

    if (!carregando && clas.length === 0) {
        return (
            <EstadoVazio
                icone="🛡️"
                titulo="Nenhum clã criado ainda."
                descricao="Assim que jogadores criarem clãs (ou o mod sincronizar grupos), eles aparecem aqui."
            />
        );
    }

    return (
        <>
            <div className="lista-usuarios-admin">
                {clas.map(cla => (
                    <div
                        key={cla.id}
                        className="usuario-admin-card"
                        onClick={() => setClaSelecionado(cla.id)}
                    >
                        {cla.estandarte
                            ? <img src={cla.estandarte} alt={cla.nome} />
                            : <span className="cla-admin-emblema-vazio">🛡️</span>}

                        <div className="usuario-admin-info">
                            <span className="usuario-admin-nome">{cla.nome}</span>
                            <span className="usuario-admin-steamid">Líder: {cla.liderNome}</span>
                        </div>

                        <div className="usuario-admin-badges">
                            <span className="badge-coins">{cla.totalMembros} {cla.totalMembros === 1 ? "membro" : "membros"}</span>
                            {cla.grupoModId && <span className="badge-vip">Sincronizado do jogo</span>}
                        </div>
                    </div>
                ))}
            </div>

            {claSelecionado && (
                <ClaAdminModal
                    claId={claSelecionado}
                    onClose={() => setClaSelecionado(null)}
                    onChange={carregar}
                />
            )}
        </>
    );
}
