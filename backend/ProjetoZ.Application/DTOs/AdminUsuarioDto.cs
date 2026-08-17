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

        public bool Banido { get; set; }

        public string? BanidoMotivo { get; set; }
    }

    public class AdminUsuarioDetalheDto : AdminUsuarioDto
    {
        public List<PlayerInventoryItemDto> Inventario { get; set; } = new();

        public List<SeguroAtivoDto> Seguros { get; set; } = new();

        public List<AdminCompraDto> Compras { get; set; } = new();
    }

    public class AdminCompraDto
    {
        public string Tipo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int Coins { get; set; }

        public decimal? ValorReais { get; set; }

        public DateTime CriadoEm { get; set; }
    }
}
