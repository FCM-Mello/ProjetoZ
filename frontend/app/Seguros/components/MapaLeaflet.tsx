"use client";

import { useEffect, useRef } from "react";
import type * as LeafletTypes from "leaflet";
import "leaflet/dist/leaflet.css";
import "./MapaLeaflet.css";

// Mapa 2D com tiles reais (zoom sem perda de nitidez), trocando o mapa 3D
// falso/verdadeiro anterior. Tiles gerados localmente a partir da imagem de
// satélite com os nomes das cidades (ver scripts-temp/gerar_tiles.mjs,
// script não versionado — resultado fica em public/Tiles).
//
// L é importado dinamicamente dentro do useEffect porque Leaflet acessa
// `window` na inicialização e quebraria a renderização no servidor (Next.js
// ainda pré-renderiza client components uma vez no server).
const TAMANHO_MUNDO_METROS = 15360;
const TAMANHO_IMAGEM_PX = 7200; // resolução real da textura, sem o padding até 8192
const MAX_ZOOM = 5;

// No L.CRS.Simple, lat/lng são pixels na escala do ZOOM 0, não do zoom
// máximo — em zoom Z, 1 unidade de lat/lng vira 2^Z pixels na tela. Como
// gerei os tiles pensando em pixels "reais" (na escala do zoom máximo),
// preciso converter dividindo por essa escala antes de virar lat/lng.
const ESCALA_ZOOM_MAX = 2 ** MAX_ZOOM;

// Calibração empírica: um veículo observado na costa (Z baixo) aparecia
// consistentemente ~2 quadrantes (2000m) mais ao norte no mapa do que a
// posição real. Corrige deslocando o Z usado na conversão — se surgir mais
// referência real (outro ponto conhecido), reajustar este valor.
const AJUSTE_Z_METROS = 2000;

const COR_MARCADOR = "#ff9f1c";
const COR_MARCADOR_ATIVO = "#38bdf8";

interface VeiculoMapa {
    idSeguro: string;
    nome: string;
    x: number;
    z: number;
}

interface MapaLeafletProps {
    veiculos: VeiculoMapa[];
    selecionado: string | null;
    onSelecionar: (idSeguro: string | null) => void;
}

// Converte metros do mundo (0-15360) pro sistema lat/lng que o L.CRS.Simple
// usa como pixel — linha 0 da imagem (topo = norte) vira lat 0, linhas
// abaixo viram lat negativo (convenção padrão do Leaflet pra imagens onde Y
// cresce pra baixo).
function mundoParaLatLng(L: typeof LeafletTypes, x: number, z: number): LeafletTypes.LatLng {
    const zAjustado = z - AJUSTE_Z_METROS;
    const pxX = ((x / TAMANHO_MUNDO_METROS) * TAMANHO_IMAGEM_PX) / ESCALA_ZOOM_MAX;
    const pxYDoTopo = (TAMANHO_IMAGEM_PX - (zAjustado / TAMANHO_MUNDO_METROS) * TAMANHO_IMAGEM_PX) / ESCALA_ZOOM_MAX;
    return L.latLng(-pxYDoTopo, pxX);
}

// Inverso de mundoParaLatLng — usado pro indicador de coordenada no rodapé.
function latLngParaMundo(latlng: LeafletTypes.LatLng): { x: number; z: number } {
    const pxX = latlng.lng * ESCALA_ZOOM_MAX;
    const pxYDoTopo = -latlng.lat * ESCALA_ZOOM_MAX;
    return {
        x: (pxX / TAMANHO_IMAGEM_PX) * TAMANHO_MUNDO_METROS,
        z: ((TAMANHO_IMAGEM_PX - pxYDoTopo) / TAMANHO_IMAGEM_PX) * TAMANHO_MUNDO_METROS + AJUSTE_Z_METROS,
    };
}

// Grade de coordenadas estilo DayZ — células de 1km, com o rótulo de 2
// dígitos que os jogadores usam pra se localizar (ex: "04 11").
const PASSO_GRADE_METROS = 1000;

function criarGrade(L: typeof LeafletTypes): LeafletTypes.LayerGroup {
    const grupo = L.layerGroup();
    const linhas: LeafletTypes.LatLngExpression[][] = [];

    for (let m = 0; m <= TAMANHO_MUNDO_METROS; m += PASSO_GRADE_METROS) {
        linhas.push([mundoParaLatLng(L, m, 0), mundoParaLatLng(L, m, TAMANHO_MUNDO_METROS)]);
        linhas.push([mundoParaLatLng(L, 0, m), mundoParaLatLng(L, TAMANHO_MUNDO_METROS, m)]);
    }

    // Só linhas, bem discretas — os rótulos numéricos ("04 11") competiam
    // visualmente com os nomes das cidades que já estão na própria imagem,
    // que são a referência que importa de verdade.
    linhas.forEach(pontos => {
        L.polyline(pontos, { color: "#ffffff", weight: 1, opacity: 0.08, interactive: false }).addTo(grupo);
    });

    return grupo;
}

function criarIcone(L: typeof LeafletTypes, cor: string, ativo: boolean): LeafletTypes.DivIcon {
    const tamanho = ativo ? 22 : 16;
    return L.divIcon({
        className: "mapaLeafletPino",
        html: `<span style="display:block;width:${tamanho}px;height:${tamanho}px;border-radius:50%;background:${cor};border:2px solid #1a1004;box-shadow:0 2px 6px rgba(0,0,0,.5);"></span>`,
        iconSize: [tamanho, tamanho],
        iconAnchor: [tamanho / 2, tamanho / 2],
    });
}

export default function MapaLeaflet({ veiculos, selecionado, onSelecionar }: MapaLeafletProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const coordenadaRef = useRef<HTMLDivElement>(null);
    const mapRef = useRef<LeafletTypes.Map | null>(null);
    const leafletRef = useRef<typeof LeafletTypes | null>(null);
    const marcadoresRef = useRef<Map<string, LeafletTypes.Marker>>(new Map());
    const posicionarMarcadoresRef = useRef<() => void>(() => {});
    const onSelecionarRef = useRef(onSelecionar);
    onSelecionarRef.current = onSelecionar;
    const selecionadoRef = useRef(selecionado);
    selecionadoRef.current = selecionado;

    // Cria o mapa e o tile layer uma única vez.
    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        let cancelado = false;

        const ladoEmLatLng = TAMANHO_IMAGEM_PX / ESCALA_ZOOM_MAX;
        const bounds: LeafletTypes.LatLngBoundsExpression = [
            [-ladoEmLatLng, 0],
            [0, ladoEmLatLng],
        ];
        const centro: LeafletTypes.LatLngTuple = [-ladoEmLatLng / 2, ladoEmLatLng / 2];

        // fitBounds não centrava direito aqui — a imagem foi preenchida com
        // padding só embaixo/à direita (pra virar potência de 2 pros tiles),
        // e o Leaflet parecia levar esse padding em conta na hora de
        // encaixar, empurrando o conteúdo real pra fora do centro. setView
        // com o centro calculado manualmente evita esse problema. O zoom é
        // arredondado pra CIMA (cobre o container inteiro, com uma cortada
        // discreta nas bordas em vez de sobrar espaço vazio).
        function ajustarView(map: LeafletTypes.Map) {
            const tamanhoPx = Math.max(container!.clientWidth, container!.clientHeight);
            if (tamanhoPx <= 0) return;

            const zoomIdeal = Math.log2(tamanhoPx / ladoEmLatLng);
            const zoom = Math.min(MAX_ZOOM, Math.max(0, Math.ceil(zoomIdeal)));
            map.setView(centro, zoom, { animate: false });
        }

        import("leaflet").then(mod => {
            if (cancelado || !container) return;

            const L = mod.default;
            leafletRef.current = L;

            const map = L.map(container, {
                crs: L.CRS.Simple,
                minZoom: 0,
                maxZoom: MAX_ZOOM,
                // Sem maxBounds — ele brigava com fitBounds: como os limites
                // do conteúdo real são menores que o container na maioria
                // dos zooms, o Leaflet forçava o zoom de volta pra 0 pra
                // nunca deixar o usuário ver além do limite, deixando o mapa
                // "solto"/desalinhado dentro do wrapper.
                attributionControl: false,
                zoomControl: false,
            });
            mapRef.current = map;

            L.tileLayer("/Tiles/{z}/{x}/{y}.jpg", {
                minZoom: 0,
                maxZoom: MAX_ZOOM,
                tileSize: 256,
                noWrap: true,
                bounds,
            }).addTo(map);

            L.control.zoom({ position: "bottomright" }).addTo(map);
            criarGrade(L).addTo(map);

            ajustarView(map);
            map.on("click", () => onSelecionarRef.current(null));

            // Indicador de coordenada no rodapé — atualizado direto no DOM
            // (sem passar por state do React) porque mousemove dispara muito
            // rápido pra justificar um re-render a cada evento.
            map.on("mousemove", evento => {
                if (!coordenadaRef.current) return;
                const { x, z } = latLngParaMundo(evento.latlng);
                coordenadaRef.current.textContent = `X ${Math.round(x)}  Z ${Math.round(z)}`;
            });
            map.on("mouseout", () => {
                if (coordenadaRef.current) coordenadaRef.current.textContent = "";
            });

            posicionarMarcadoresRef.current();
        });

        // Leaflet só mede o tamanho do container na inicialização — se isso
        // acontecer antes do layout (grid/flex) terminar de assentar, o mapa
        // fica com um tamanho errado. O ResizeObserver às vezes dispara em
        // medições intermediárias (layout ainda assentando), então usamos
        // debounce — só reajusta quando o tamanho parar de mudar por 150ms.
        let debounceId: ReturnType<typeof setTimeout> | undefined;
        const resizeObserver = new ResizeObserver(() => {
            const map = mapRef.current;
            if (!map) return;

            map.invalidateSize();

            clearTimeout(debounceId);
            debounceId = setTimeout(() => ajustarView(map), 150);
        });
        resizeObserver.observe(container);

        return () => {
            cancelado = true;
            clearTimeout(debounceId);
            resizeObserver.disconnect();
            mapRef.current?.remove();
            mapRef.current = null;
        };
    }, []);

    // Marcadores — recriados sempre que a lista de veículos muda (e também
    // chamado assim que o mapa termina de carregar, caso os veículos já
    // tenham chegado antes disso).
    useEffect(() => {
        function posicionar() {
            const L = leafletRef.current;
            const map = mapRef.current;
            if (!L || !map) return;

            marcadoresRef.current.forEach(marker => marker.remove());
            marcadoresRef.current.clear();

            veiculos.forEach(veiculo => {
                const posicao = mundoParaLatLng(L, veiculo.x, veiculo.z);
                const ativo = veiculo.idSeguro === selecionadoRef.current;
                const marker = L.marker(posicao, {
                    icon: criarIcone(L, ativo ? COR_MARCADOR_ATIVO : COR_MARCADOR, ativo),
                });

                marker.on("click", evento => {
                    L.DomEvent.stopPropagation(evento);
                    const jaEstavaSelecionado = selecionadoRef.current === veiculo.idSeguro;
                    onSelecionarRef.current(jaEstavaSelecionado ? null : veiculo.idSeguro);
                });

                marker.addTo(map);
                marcadoresRef.current.set(veiculo.idSeguro, marker);
            });
        }

        posicionarMarcadoresRef.current = posicionar;
        posicionar();
    }, [veiculos]);

    // Realce do marcador selecionado.
    useEffect(() => {
        const L = leafletRef.current;
        if (!L) return;

        marcadoresRef.current.forEach((marker, idSeguro) => {
            const ativo = idSeguro === selecionado;
            marker.setIcon(criarIcone(L, ativo ? COR_MARCADOR_ATIVO : COR_MARCADOR, ativo));
            marker.setZIndexOffset(ativo ? 1000 : 0);
        });
    }, [selecionado]);

    return (
        <div className="mapaLeafletWrapper">
            <div ref={containerRef} className="mapaLeafletContainer" />
            <div ref={coordenadaRef} className="mapaLeafletCoordenada" />
        </div>
    );
}
