using System.Text.Json;

namespace ProjetoZ.Api.Services;

public class AdminsProvider
{
    private readonly HashSet<string> _adminSteamIds;

    public AdminsProvider(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "admins.json");

        var json = File.Exists(path) ? File.ReadAllText(path) : "[]";

        _adminSteamIds = (JsonSerializer.Deserialize<List<string>>(json) ?? [])
            .ToHashSet();
    }

    public bool IsAdmin(string? steamId) =>
        steamId != null && _adminSteamIds.Contains(steamId);
}
