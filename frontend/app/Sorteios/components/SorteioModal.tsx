"use client";

import { useState } from "react";
import { Product } from "../../models/Product";
import { VipTier } from "../../models/VipTier";
import { CreateSorteioRequest } from "../../models/Sorteio";
import { useToast } from "../../contexts/ToastContext";
import "../../components/Modal.css";
import "./SorteioModal.css";

interface Props {
    produtos: Product[];
    vipTiers: VipTier[];
    onClose: () => void;
    onSave: (request: CreateSorteioRequest) => void;
}

export default function SorteioModal({ produtos, vipTiers, onClose, onSave }: Props) {
    const [titulo, setTitulo] = useState("");
    const [descricao, setDescricao] = useState("");
    const [premioVipNivel, setPremioVipNivel] = useState<number | null>(null);
    const [produtosSelecionados, setProdutosSelecionados] = useState<string[]>([]);
    const { erro: mostrarErro } = useToast();

    function toggleProduto(id: string) {
        setProdutosSelecionados(prev =>
            prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
        );
    }

    function salvar() {
        if (!titulo.trim()) return;

        if (premioVipNivel === null && produtosSelecionados.length === 0) {
            mostrarErro("Escolha ao menos um prêmio: um nível de VIP ou um ou mais produtos.");
            return;
        }

        onSave({
            titulo,
            descricao,
            premioVipNivel,
            premioProdutoIds: produtosSelecionados,
        });

        onClose();
    }

    return (
        <div className="modal-overlay">
            <div className="modal">
                <h2>Novo Sorteio</h2>

                <input
                    placeholder="Título"
                    value={titulo}
                    onChange={(e) => setTitulo(e.target.value)}
                />

                <textarea
                    placeholder="Descrição"
                    value={descricao}
                    onChange={(e) => setDescricao(e.target.value)}
                />

                <select
                    value={premioVipNivel ?? ""}
                    onChange={(e) => setPremioVipNivel(e.target.value ? Number(e.target.value) : null)}
                >
                    <option value="">Sem prêmio de VIP</option>
                    {vipTiers.map(tier => (
                        <option key={tier.nivel} value={tier.nivel}>
                            VIP {tier.nome} ({tier.duracaoDias} dias)
                        </option>
                    ))}
                </select>

                <div className="sorteioProdutosLista">
                    {produtos.length === 0 && (
                        <span className="sorteioProdutosVazio">Nenhum produto cadastrado.</span>
                    )}

                    {produtos.map(produto => (
                        <label key={produto.id} className="sorteioProdutoItem">
                            <input
                                type="checkbox"
                                checked={produtosSelecionados.includes(produto.id)}
                                onChange={() => toggleProduto(produto.id)}
                            />
                            <img src={produto.imagem} alt={produto.nome} />
                            <span>{produto.nome}</span>
                        </label>
                    ))}
                </div>

                <div className="modal-buttons">
                    <button className="btnCancel" onClick={onClose}>Cancelar</button>
                    <button className="btnSave" onClick={salvar}>Criar</button>
                </div>
            </div>
        </div>
    );
}
