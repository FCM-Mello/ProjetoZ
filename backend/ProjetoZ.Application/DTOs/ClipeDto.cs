namespace ProjetoZ.Application.DTOs
{
    public class ClipeDto
    {
        public Guid Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string AutorNome { get; set; } = string.Empty;

        public string AutorAvatar { get; set; } = string.Empty;

        public bool AutorSouEu { get; set; }

        public int Curtidas { get; set; }

        public bool JaCurti { get; set; }

        public DateTime CriadoEm { get; set; }
    }

    public class ClipesResponseDto
    {
        public DateTime ProximoFechamento { get; set; }

        public List<ClipeDto> Clipes { get; set; } = new();

        public ClipeVencedorDto? UltimoVencedor { get; set; }
    }

    public class ClipeVencedorDto
    {
        public string Titulo { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string AutorNome { get; set; } = string.Empty;

        public string AutorAvatar { get; set; } = string.Empty;

        public int Curtidas { get; set; }

        public DateTime FechadoEm { get; set; }
    }

    public class CreateClipeRequest
    {
        public string Titulo { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;
    }
}
