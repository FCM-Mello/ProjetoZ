"use client";

import { useEffect, useState } from "react";
import { getUsuarios } from "../../services/adminApi";
import { criarNotificacao, getTodasNotificacoes, excluirNotificacao } from "../../services/notificacoesApi";
import { AdminUsuario } from "../../models/AdminUsuario";
import { NotificacaoAdmin, NivelNotificacao } from "../../models/Notificacao";
import "../components/UsuarioAdminModal.css";
import "./page.css";

const NIVEIS: { valor: NivelNotificacao; nome: string }[] = [
    { valor: "verde", nome: "Verde" },
    { valor: "amarelo", nome: "Amarelo" },
    { valor: "vermelho", nome: "Vermelho" },
];

export default function AdminNotificacoes() {
    const [titulo, setTitulo] = useState("");
    const [mensagem, setMensagem] = useState("");
    const [nivel, setNivel] = useState<NivelNotificacao>("verde");
    const [paraTodos, setParaTodos] = useState(true);
    const [enviarEm, setEnviarEm] = useState("");

    const [buscaUsuario, setBuscaUsuario] = useState("");
    const [resultadosBusca, setResultadosBusca] = useState<AdminUsuario[]>([]);
    const [selecionados, setSelecionados] = useState<AdminUsuario[]>([]);

    const [historico, setHistorico] = useState<NotificacaoAdmin[]>([]);
    const [carregandoHistorico, setCarregandoHistorico] = useState(true);
    const [enviando, setEnviando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);
    const [sucesso, setSucesso] = useState<string | null>(null);

    useEffect(() => {
        carregarHistorico();
    }, []);

    useEffect(() => {
        if (paraTodos || !buscaUsuario.trim()) {
            setResultadosBusca([]);
            return;
        }

        const timeout = setTimeout(() => {
            getUsuarios(buscaUsuario).then(setResultadosBusca).catch(console.error);
        }, 300);

        return () => clearTimeout(timeout);
    }, [buscaUsuario, paraTodos]);

    async function carregarHistorico() {
        setCarregandoHistorico(true);
        try {
            setHistorico(await getTodasNotificacoes());
        } catch (e) {
            console.error(e);
        } finally {
            setCarregandoHistorico(false);
        }
    }

    function adicionarSelecionado(usuario: AdminUsuario) {
        if (!selecionados.some(u => u.id === usuario.id)) {
            setSelecionados(atual => [...atual, usuario]);
        }
        setBuscaUsuario("");
        setResultadosBusca([]);
    }

    function removerSelecionado(id: string) {
        setSelecionados(atual => atual.filter(u => u.id !== id));
    }

    async function enviar() {
        setErro(null);
        setSucesso(null);

        if (!titulo.trim() || !mensagem.trim()) {
            setErro("Preencha título e mensagem.");
            return;
        }

        if (!paraTodos && selecionados.length === 0) {
            setErro("Selecione ao menos um destinatário, ou marque para enviar a todos.");
            return;
        }

        setEnviando(true);
        try {
            await criarNotificacao({
                titulo: titulo.trim(),
                mensagem: mensagem.trim(),
                nivel,
                paraTodos,
                destinatarioUserIds: paraTodos ? undefined : selecionados.map(u => u.id),
                enviarEm: enviarEm ? new Date(enviarEm).toISOString() : undefined,
            });

            setSucesso("Notificação criada.");
            setTitulo("");
            setMensagem("");
            setNivel("verde");
            setParaTodos(true);
            setEnviarEm("");
            setSelecionados([]);
            await carregarHistorico();
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Erro ao criar notificação.");
        } finally {
            setEnviando(false);
        }
    }

    async function excluir(id: string) {
        try {
            await excluirNotificacao(id);
            await carregarHistorico();
        } catch (e) {
            console.error(e);
        }
    }

    function formatarData(data: string) {
        return new Date(data).toLocaleString("pt-BR");
    }

    return (
        <div className="containerAdminNotificacoes">
            <div className="notificacaoForm">
                {erro && <div className="admin-modal-erro">{erro}</div>}
                {sucesso && <div className="notificacaoSucesso">{sucesso}</div>}

                <input
                    className="notificacaoInput"
                    placeholder="Título"
                    value={titulo}
                    onChange={(e) => setTitulo(e.target.value)}
                />

                <textarea
                    className="notificacaoInput notificacaoTextarea"
                    placeholder="Mensagem"
                    value={mensagem}
                    onChange={(e) => setMensagem(e.target.value)}
                />

                <div className="notificacaoNiveis">
                    {NIVEIS.map(n => (
                        <button
                            key={n.valor}
                            type="button"
                            className={`notificacaoNivelBotao notificacaoNivelBotao-${n.valor} ${nivel === n.valor ? "notificacaoNivelBotao-ativo" : ""}`}
                            onClick={() => setNivel(n.valor)}
                        >
                            {n.nome}
                        </button>
                    ))}
                </div>

                <label className="notificacaoParaTodos">
                    <input
                        type="checkbox"
                        checked={paraTodos}
                        onChange={(e) => setParaTodos(e.target.checked)}
                    />
                    Enviar para todos os usuários
                </label>

                {!paraTodos && (
                    <div className="notificacaoDestinatarios">
                        <input
                            className="notificacaoInput"
                            placeholder="Buscar usuário por nome ou SteamID..."
                            value={buscaUsuario}
                            onChange={(e) => setBuscaUsuario(e.target.value)}
                        />

                        {resultadosBusca.length > 0 && (
                            <ul className="notificacaoBuscaResultados">
                                {resultadosBusca.map(u => (
                                    <li key={u.id} onClick={() => adicionarSelecionado(u)}>
                                        {u.nome} <span>{u.steamId}</span>
                                    </li>
                                ))}
                            </ul>
                        )}

                        {selecionados.length > 0 && (
                            <div className="notificacaoChips">
                                {selecionados.map(u => (
                                    <span key={u.id} className="notificacaoChip">
                                        {u.nome}
                                        <button type="button" onClick={() => removerSelecionado(u.id)}>×</button>
                                    </span>
                                ))}
                            </div>
                        )}
                    </div>
                )}

                <label className="notificacaoAgendar">
                    Enviar em (opcional — em branco envia imediatamente)
                    <input
                        type="datetime-local"
                        className="notificacaoInput"
                        value={enviarEm}
                        onChange={(e) => setEnviarEm(e.target.value)}
                    />
                </label>

                <button className="notificacaoEnviarBotao" disabled={enviando} onClick={enviar}>
                    {enviando ? "Enviando..." : "Criar notificação"}
                </button>
            </div>

            <h3 className="notificacaoHistoricoTitulo">Histórico</h3>

            {carregandoHistorico ? (
                <p className="admin-lista-vazia">Carregando...</p>
            ) : historico.length === 0 ? (
                <p className="admin-lista-vazia">Nenhuma notificação criada ainda.</p>
            ) : (
                <div className="notificacaoHistoricoLista">
                    {historico.map(n => (
                        <div key={n.id} className={`notificacaoHistoricoItem notificacaoHistoricoItem-${n.nivel}`}>
                            <div className="notificacaoHistoricoInfo">
                                <strong>{n.titulo}</strong>
                                <p>{n.mensagem}</p>
                                <span>
                                    {n.paraTodos ? "Todos" : `${n.totalDestinatarios} destinatário(s)`}
                                    {" · "}{n.totalLeituras} leitura(s)
                                    {" · "}envia em {formatarData(n.enviarEm)}
                                    {" · "}expira em {formatarData(n.expiraEm)}
                                </span>
                            </div>

                            <button className="admin-btn-perigo notificacaoExcluirBotao" onClick={() => excluir(n.id)}>
                                Excluir
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
