namespace ProjetoZ.Application.DTOs
{
    public class PlayerLookupRequest
    {
        public string SteamId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
    }
}
