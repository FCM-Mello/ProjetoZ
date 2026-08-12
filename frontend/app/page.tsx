import Link from "next/link";
import "./page.css";

export default function Home() {
  return (
    <div className="center-card">
      <h1 className="landingTitulo">ArkZ</h1>

      <p className="landingSubtitulo">
        Loja oficial do nosso servidor privado de DayZ. Faça login com sua conta Steam pra comprar
        Az Coins, itens exclusivos e planos VIP, participar de sorteios e concorrer no ranking
        semanal de clipes.
      </p>

      <ul className="landingLista">
        <li>🪙 Az Coins — moeda do servidor, compre com dinheiro real via Mercado Pago</li>
        <li>★ Planos VIP com benefícios exclusivos dentro do jogo</li>
        <li>🎁 Sorteios de itens e VIP entre os jogadores</li>
        <li>🎬 Ranking semanal de clipes, com premiação em Az Coins</li>
      </ul>

      <Link href="/Home" className="buttonConfirm">Entrar na loja</Link>
    </div>
  );
}