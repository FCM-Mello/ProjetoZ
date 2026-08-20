import Link from "next/link";
import "./page.css";

export default function Home() {
  return (
    <div className="landingViewport">
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

        <p className="landingDados">
          Pra participar do ranking de clipes, pedimos acesso somente-leitura à sua conta do
          Google (escopo <code>youtube.readonly</code>), usado só pra confirmar que o vídeo
          postado é do seu próprio canal do YouTube — não lemos, alteramos nem publicamos nada
          na sua conta. Veja todos os detalhes na nossa{" "}
          <Link href="/Privacidade">Política de Privacidade</Link>.
        </p>

        <Link href="/Home" className="buttonConfirm">Entrar na loja</Link>
      </div>
    </div>
  );
}
