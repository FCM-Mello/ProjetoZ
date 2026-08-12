namespace ProjetoZ.Application.DTOs
{
    public class AdminUsuarioDto
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; } = string.Empty;

        public string Nome { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public int Coins { get; set; }

        public int VipNivel { get; set; }

        public string? VipNivelNome { get; set; }

        public DateTime? VipExpiraEm { get; set; }

        public bool IsAdmin { get; set; }
    }

    public class AdminUsuarioDetalheDto : AdminUsuarioDto
    {
        public List<PlayerInventoryItemDto> Inventario { get; set; } = new();
    }
}
