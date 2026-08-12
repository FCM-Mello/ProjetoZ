namespace ProjetoZ.Application.DTOs
{
    public class AjustarCoinsRequest
    {
        public int Delta { get; set; }
    }

    public class DefinirVipRequest
    {
        public int Nivel { get; set; }
    }

    public class AdicionarInventarioRequest
    {
        public Guid ProdutoId { get; set; }
    }
}
