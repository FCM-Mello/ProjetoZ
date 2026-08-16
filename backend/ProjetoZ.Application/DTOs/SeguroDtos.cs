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
    }

    public class SeguroDto
    {
        public Guid IdSeguro { get; set; }

        public string Id { get; set; } = string.Empty;

        public bool PodeResgatar { get; set; }

        // Nulo quando PodeResgatar é true.
        public DateTime? ProximoResgateEm { get; set; }
    }
}
