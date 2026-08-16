"use client";

import { useEffect, useState } from "react";
import { getMeusSeguros, SeguroAtivo } from "../services/segurosApi";
import { useRequireAuth } from "../hooks/useRequireAuth";
import "./page.css";

// Chernarus+ tem 15360x15360 metros de mundo. A imagem do mapa cobre esse
// mundo inteiro de ponta a ponta, então a conversão é uma regra de três
// simples. Z cresce pra norte no jogo, mas a imagem cresce pra baixo — daí o
// "100 -" na vertical.
const TAMANHO_MUNDO_METROS = 15360;

function posicaoNoMapa(x: number, z: number) {
    return {
        left: `${(x / TAMANHO_MUNDO_METROS) * 100}%`,
        top: `${100 - (z / TAMANHO_MUNDO_METROS) * 100}%`,
    };
}

function formatarData(data: string) {
    return new Date(data).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" });
}

function minutosAtras(data: string) {
    return Math.floor((Date.now() - new Date(data).getTime()) / 60000);
}

export default function Seguros() {
    useRequireAuth();

    const [seguros, setSeguros] = useState<SeguroAtivo[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);
    const [selecionado, setSelecionado] = useState<string | null>(null);

    useEffect(() => {
        carregar();
    }, []);

    async function carregar() {
        setCarregando(true);
        setErro(null);

        try {
            const dados = await getMeusSeguros();
            setSeguros(dados);
        } catch (e) {
            setErro(e instanceof Error ? e.message : "Não foi possível carregar seus seguros.");
        } finally {
            setCarregando(false);
        }
    }

    const comPosicao = seguros.filter(s => s.posicaoX != null && s.posicaoZ != null);

    return (
        <main className="containerSeguros">
            <h2 className="section-title">Seguros ativos</h2>
            <p className="segurosSubtitulo">
                Veículos e itens segurados no jogo — cada seguro dura 1 mês. A posição no mapa é sincronizada
                pelo servidor a cada ~15 minutos.
            </p>

            {carregando && <p className="segurosEstado">Carregando...</p>}
            {erro && <p className="segurosEstado segurosEstado-erro">{erro}</p>}

            {!carregando && !erro && seguros.length === 0 && (
                <p className="segurosEstado">Você não tem nenhum seguro ativo no momento.</p>
            )}

            {!carregando && !erro && seguros.length > 0 && (
                <div className="segurosLayout">
                    <div className="mapaWrapper">
                        <img src="/Images/mapa-chernarus.jpg" alt="Mapa do Chernarus" className="mapaImagem" />

                        {comPosicao.map(seguro => {
                            const pos = posicaoNoMapa(seguro.posicaoX!, seguro.posicaoZ!);
                            const ativo = selecionado === seguro.idSeguro;

                            return (
                                <button
                                    key={seguro.idSeguro}
                                    className={`mapaMarcador ${ativo ? "mapaMarcador-ativo" : ""}`}
                                    style={pos}
                                    onClick={() => setSelecionado(ativo ? null : seguro.idSeguro)}
                                    title={seguro.veiculoNome ?? seguro.id}
                                >
                                    <span className="mapaMarcadorPino" />
                                    {ativo && (
                                        <span className="mapaMarcadorTooltip">
                                            <strong>{seguro.veiculoNome ?? seguro.id}</strong>
                                            {seguro.posicaoGrid && <span>Grid {seguro.posicaoGrid}</span>}
                                            {seguro.posicaoAtualizadaEm && (
                                                <span>Atualizado há {minutosAtras(seguro.posicaoAtualizadaEm)} min</span>
                                            )}
                                        </span>
                                    )}
                                </button>
                            );
                        })}
                    </div>

                    <div className="segurosLista">
                        {seguros.map(seguro => {
                            const desatualizado = seguro.posicaoAtualizadaEm != null && minutosAtras(seguro.posicaoAtualizadaEm) > 30;

                            return (
                                <div
                                    key={seguro.idSeguro}
                                    className={`seguroCard ${seguro.posicaoX != null ? "seguroCard-clicavel" : ""} ${selecionado === seguro.idSeguro ? "seguroCard-ativo" : ""}`}
                                    onClick={() => seguro.posicaoX != null && setSelecionado(seguro.idSeguro)}
                                >
                                    <div className="seguroCardTopo">
                                        <span className="seguroCardNome">{seguro.veiculoNome ?? seguro.id}</span>
                                        <span className="seguroCardExpira">até {formatarData(seguro.expiraEm)}</span>
                                    </div>

                                    {seguro.posicaoGrid ? (
                                        <div className="seguroCardPosicao">
                                            <span>Grid {seguro.posicaoGrid}</span>
                                            <span className={desatualizado ? "seguroCardDesatualizado" : ""}>
                                                {desatualizado ? "Posição desatualizada" : `Atualizado há ${minutosAtras(seguro.posicaoAtualizadaEm!)} min`}
                                            </span>
                                        </div>
                                    ) : (
                                        <span className="seguroCardAguardando">Aguardando sincronização de posição...</span>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}
        </main>
    );
}
