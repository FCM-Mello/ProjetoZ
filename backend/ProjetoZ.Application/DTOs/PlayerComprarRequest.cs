namespace ProjetoZ.Application.DTOs
{
    public class PlayerComprarRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string ItemId { get; set; } = string.Empty;

        public string ItemNome { get; set; } = string.Empty;

        public int Preco { get; set; }
    }
}
