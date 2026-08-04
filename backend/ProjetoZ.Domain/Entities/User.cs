namespace ProjetoZ.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string SteamId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}