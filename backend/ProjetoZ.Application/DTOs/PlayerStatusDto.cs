namespace ProjetoZ.Application.DTOs
{
    public class PlayerStatusDto
    {
        public string SteamId { get; set; } = string.Empty;

        public bool Vip { get; set; }

        public int Coins { get; set; }

        public List<PlayerInventoryItemDto> Inventario { get; set; } = new();
    }

    public class PlayerInventoryItemDto
    {
        public Guid ProdutoId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int Quantidade { get; set; }
    }
}
