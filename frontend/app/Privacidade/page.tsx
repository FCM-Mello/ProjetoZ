import Link from "next/link";
import "./page.css";

export const metadata = {
    title: "Política de Privacidade — ArkZ",
};

export default function Privacidade() {
    return (
        <main className="containerPrivacidade">
            <h2 className="section-title">Política de Privacidade</h2>
            <p className="privacidadeAtualizado">Última atualização: agosto de 2026</p>

            <section>
                <h3>1. Quem somos</h3>
                <p>
                    O ArkZ (arkz.dev.br) é a loja e central de serviços de um servidor privado do jogo DayZ.
                    Esta página explica quais dados coletamos, por que coletamos e como você pode falar
                    conosco sobre eles.
                </p>
            </section>

            <section>
                <h3>2. Dados que coletamos</h3>
                <ul>
                    <li><strong>Perfil da Steam:</strong> ao fazer login, recebemos seu SteamID, nome de exibição, avatar e link do perfil, fornecidos pela própria Steam.</li>
                    <li><strong>Dados de uso do site:</strong> saldo de Az Coins, itens comprados, nível de VIP, participação em sorteios e histórico de compras.</li>
                    <li><strong>Dados de pagamento:</strong> compras de Az Coins com dinheiro real são processadas pelo Mercado Pago — não armazenamos número de cartão nem dados bancários, apenas o registro da transação.</li>
                    <li><strong>Canal do YouTube:</strong> se você vincular sua conta do Google para participar do ranking semanal de clipes, guardamos apenas o ID e o nome do seu canal — o suficiente para confirmar que um vídeo postado é realmente seu. Não acessamos, gerenciamos nem publicamos nada na sua conta do YouTube.</li>
                </ul>
            </section>

            <section>
                <h3>3. Como usamos esses dados</h3>
                <p>
                    Usamos seus dados exclusivamente para operar o site: autenticar seu login, manter seu
                    saldo e inventário, processar compras, exibir o ranking de clipes e integrar com o
                    servidor de jogo (por exemplo, pra liberar benefícios de VIP dentro do jogo). Não
                    vendemos nem compartilhamos seus dados com terceiros além dos serviços necessários para
                    o funcionamento do site (Steam, Mercado Pago e Google/YouTube, cada um regido por sua
                    própria política de privacidade).
                </p>
            </section>

            <section>
                <h3>4. Acesso ao YouTube</h3>
                <p>
                    O vínculo com o YouTube usa o escopo somente-leitura <code>youtube.readonly</code> do
                    Google. Ele é usado uma única vez, no momento da vinculação, apenas para identificar o
                    canal associado à sua conta Google. Esse mesmo ID é então comparado com o vídeo que você
                    posta no ranking semanal, pra confirmar que o clipe é seu. Não usamos esse acesso pra
                    nenhuma outra finalidade.
                </p>
            </section>

            <section>
                <h3>5. Retenção e exclusão</h3>
                <p>
                    Clipes e curtidas da semana são apagados automaticamente toda virada de semana, após o
                    fechamento do ranking. Os demais dados (perfil, coins, inventário, histórico de compras)
                    ficam armazenados enquanto sua conta existir no site. Você pode pedir a exclusão dos seus
                    dados a qualquer momento pelo contato abaixo.
                </p>
            </section>

            <section>
                <h3>6. Contato</h3>
                <p>
                    Dúvidas sobre privacidade ou pedidos de exclusão de dados podem ser enviados para{" "}
                    <a href="mailto:filipemello.cunha@gmail.com">filipemello.cunha@gmail.com</a>.
                </p>
            </section>

            <Link href="/Home" className="privacidadeVoltar">← Voltar pra loja</Link>
        </main>
    );
}
