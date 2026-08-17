namespace ProjetoZ.Application.DTOs
{
    public class CriarSeguroRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        // Id do item no ArkZ_Catalogo.c do mod (ex: "carro"). Não confundir
        // com o Id do registro de seguro, que é gerado aqui e devolvido
        // na resposta como idSeguro.
        public string Id { get; set; } = string.Empty;

        // Opcional — id do veículo específico no mundo do jogo. Se o mod já
        // sabe qual veículo está sendo segurado no momento da compra, manda
        // aqui e o vínculo é feito na hora, sem depender da sincronização de
        // posição pra descobrir isso depois (que ainda funciona como
        // fallback pra seguros criados sem esse campo).
        public string? CarroId { get; set; }
    }

    public class ListaSegurosRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
    }

    public class ResgatarSeguroRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public Guid IdSeguro { get; set; }

        // Resgate expresso: o mod já debitou o preço via /api/game/comprar e
        // manda true pra pular o cooldown. Só o servidor do mod envia isso —
        // o cliente do jogo não fala direto com essa API.
        public bool Pago { get; set; }

        // Opcional — o resgate recria o veículo no jogo, então o CarroId
        // antigo deixa de existir e o mod manda o novo aqui pra manter o
        // vínculo (senão a sincronização de posição só ia religar sozinha
        // no próximo lote, e o seguro ficaria "sem carro" até lá).
        public string? CarroId { get; set; }
    }

    public class SeguroDto
    {
        public Guid IdSeguro { get; set; }

        public string Id { get; set; } = string.Empty;

        public bool PodeResgatar { get; set; }

        // Nulo quando PodeResgatar é true.
        public DateTime? ProximoResgateEm { get; set; }
    }

    // Job do mod roda a cada ~15min e manda a posição de todos os veículos
    // segurados de todos os jogadores numa única chamada em lote.
    public class SincronizarPosicoesRequest
    {
        public string ApiKey { get; set; } = string.Empty;

        public List<VeiculoPosicaoRequestDto> Veiculos { get; set; } = new();
    }

    public class VeiculoPosicaoRequestDto
    {
        // Id do veículo específico no mundo do jogo — não é o Id do seguro
        // nem o ItemId do catálogo.
        public string CarroId { get; set; } = string.Empty;

        public string SteamId { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string PosicaoGrid { get; set; } = string.Empty;

        public double X { get; set; }

        public double Z { get; set; }
    }

    // Devolvido pelo GET /api/seguros/meus (autenticado por JWT, consumido
    // pelo site) — não confundir com SeguroDto, que é a resposta pro mod.
    public class SeguroAtivoDto
    {
        public Guid IdSeguro { get; set; }

        public string Id { get; set; } = string.Empty;

        public DateTime ExpiraEm { get; set; }

        public string? CarroId { get; set; }

        public string? VeiculoNome { get; set; }

        public string? PosicaoGrid { get; set; }

        public double? PosicaoX { get; set; }

        public double? PosicaoZ { get; set; }

        public DateTime? PosicaoAtualizadaEm { get; set; }
    }
}
