"use client";

import { useEffect, useState } from "react";
import { getProducts, createProduct, deleteProduct } from "../services/productsApi";
import { useRouter } from "next/navigation";
import { Product } from "../models/Product";
import ProductModal from "../Components/ProductModal";
import "./page.css";

export default function Home() {
    const [search, setSearch] = useState("");
    const [produtos, setProdutos] = useState<Product[]>([]);
    const [showModal, setShowModal] = useState(false);
    const router = useRouter();

    useEffect(() => {
        const token = localStorage.getItem("token");

        if (!token) {
            router.push("/Auth/Login");
        }

    }, []);

    useEffect(() => {
        carregarProdutos();
    }, []);

    async function salvarProduto(product: Product) {
        try {
            await createProduct(product);
            alert("Produto cadastrado!");
            carregarProdutos();
        } catch (e) {
            console.error(e);
            alert("Erro ao cadastrar.");
        }
    }


    async function carregarProdutos() {
        try {
            const dados = await getProducts();
            setProdutos(dados);
        }
            catch (e) {
            console.error(e);
        }
    }

    function excluirProduto() {
        console.log("Excluir");
    }

    return (
    <main className="containerHome">
<div className="toolbar">

                <input
                    className="search"
                    type="text"
                    placeholder="Pesquisar..."
                    value={search}
                    onChange={(e)=>setSearch(e.target.value)}
                />

                <div className="toolbar-buttons">

                    <button
                        className="btnCreate"
                        onClick={() => setShowModal(true)}>
                        Criar
                    </button>

                    <button
                        className="btnDelete"
                        onClick={excluirProduto}>
                        Excluir
                    </button>

                </div>

            </div>



      <div className="grid-produtos">
        {produtos
    .filter(x =>
        x.nome.toLowerCase().includes(search.toLowerCase()))
    .map(produto => (

        <div className="card" key={produto.id}>
            <img src={produto.imagem} />

            <div className="card-body">
               <h3>{produto.nome}</h3>

<p>{produto.descricao}</p>

<span>R$ {produto.preco.toFixed(2)}</span>

<small>Estoque: {produto.estoque}</small>

<button>Comprar</button>
            </div>

        </div>

))}
      </div>
          {showModal && (
    <ProductModal
        onClose={() => setShowModal(false)}
        onSave={salvarProduto}
    />
)}
    </main>


  );
}