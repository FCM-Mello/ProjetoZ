import Link from "next/link";
import "./page.css";

export const metadata = {
    title: "Termos de Serviço",
    description: "Regras de uso do ArkZ: Az Coins, VIP, sorteios, ranking de clipes e conduta esperada dos usuários.",
};

export default function Termos() {
    return (
        <main className="containerTermos">
            <h2 className="section-title">Termos de Serviço</h2>
            <p className="termosAtualizado">Última atualização: agosto de 2026</p>

            <section>
                <h3>1. Aceitação</h3>
                <p>
                    Ao fazer login e usar o ArkZ (arkz.dev.br), você concorda com estes termos. Se não
                    concordar, não utilize o site. O ArkZ é a loja e central de serviços de um servidor
                    privado do jogo DayZ, sem qualquer vínculo com a Bohemia Interactive.
                </p>
            </section>

            <section>
                <h3>2. Conta</h3>
                <p>
                    O acesso é feito exclusivamente via login com sua conta Steam. Você é responsável por
                    manter sua conta Steam segura — qualquer atividade realizada através dela no ArkZ é de
                    sua responsabilidade.
                </p>
            </section>

            <section>
                <h3>3. Az Coins</h3>
                <p>
                    Az Coins são uma moeda virtual de uso exclusivo dentro do ArkZ, sem valor monetário fora
                    da plataforma. Podem ser compradas com dinheiro real através do Mercado Pago, sujeitas
                    aos preços vigentes no momento da compra, que podem mudar sem aviso prévio. Az Coins não
                    são transferíveis entre contas e não são resgatáveis em dinheiro.
                </p>
            </section>

            <section>
                <h3>4. Compras e reembolso</h3>
                <p>
                    Itens, planos VIP e Az Coins são bens digitais entregues imediatamente após a
                    confirmação do pagamento. Por serem de consumo imediato, compras já processadas
                    geralmente não são reembolsáveis, exceto nos casos previstos pelo Código de Defesa do
                    Consumidor. Problemas com pagamento ou entrega devem ser reportados pelo contato abaixo.
                </p>
            </section>

            <section>
                <h3>5. VIP</h3>
                <p>
                    Os planos VIP concedem benefícios exclusivos dentro do jogo por 30 dias a partir da
                    ativação (compra, sorteio ganho ou concessão administrativa). O VIP não é transferível e
                    pode ser revogado em caso de violação destes termos.
                </p>
            </section>

            <section>
                <h3>6. Sorteios</h3>
                <p>
                    A participação em sorteios é gratuita. O vencedor é escolhido aleatoriamente entre os
                    participantes inscritos no momento do sorteio. Prêmios (VIP e/ou produtos) são
                    concedidos automaticamente ao vencedor e não são transferíveis nem trocáveis por Az
                    Coins.
                </p>
            </section>

            <section>
                <h3>7. Ranking semanal de clipes</h3>
                <p>
                    Só é permitido postar vídeos do seu próprio canal do YouTube, verificado via vínculo com
                    sua conta Google, publicados durante a semana atual do ranking e com "ArkZ" mencionado
                    no título do vídeo. Curtir o próprio clipe não é permitido. Ao final da semana, o autor
                    do clipe mais curtido recebe 500 Az Coins automaticamente (empates são resolvidos por
                    sorteio entre os empatados); todos os clipes e curtidas da semana são apagados em
                    seguida. Reservamo-nos o direito de remover clipes ou desqualificar participantes em
                    caso de conteúdo ofensivo, fraude de curtidas ou vídeo que não pertença ao participante.
                </p>
            </section>

            <section>
                <h3>8. Conduta</h3>
                <p>
                    É proibido explorar falhas do site ou do jogo para obter vantagem indevida, usar contas
                    falsas ou automatizadas, fraudar curtidas ou participações, e postar conteúdo ofensivo,
                    ilegal ou que viole direitos de terceiros. Violações podem resultar em suspensão da
                    conta e perda de Az Coins, VIP ou prêmios, a critério dos administradores.
                </p>
            </section>

            <section>
                <h3>9. Disponibilidade e mudanças</h3>
                <p>
                    O ArkZ é oferecido "como está". Podemos alterar, suspender ou descontinuar
                    funcionalidades, preços ou benefícios a qualquer momento. Não nos responsabilizamos por
                    indisponibilidade do servidor de jogo ou de serviços de terceiros (Steam, Mercado Pago,
                    Google/YouTube), que possuem seus próprios termos e políticas.
                </p>
            </section>

            <section>
                <h3>10. Contato</h3>
                <p>
                    Dúvidas sobre estes termos podem ser enviadas para{" "}
                    <a href="mailto:filipemello.cunha@gmail.com">filipemello.cunha@gmail.com</a>. Veja
                    também nossa <Link href="/Privacidade">Política de Privacidade</Link>.
                </p>
            </section>

            <Link href="/Home" className="termosVoltar">← Voltar pra loja</Link>
        </main>
    );
}
