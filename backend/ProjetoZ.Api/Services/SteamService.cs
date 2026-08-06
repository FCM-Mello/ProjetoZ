using ProjetoZ.Domian.Models;
using System.Text.Json;

namespace ProjetoZ.Api.Services;

public class SteamService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SteamService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<SteamProfile?> GetProfileAsync(string steamId)
    {
        var apiKey = _configuration["Steam:ApiKey"];

        var url =
            $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);

        var players = document.RootElement
            .GetProperty("response")
            .GetProperty("players");

        if (players.GetArrayLength() == 0)
            return null;

        var player = players[0];

        return new SteamProfile
        {
            SteamId = player.GetProperty("steamid").GetString() ?? "",
            Name = player.GetProperty("personaname").GetString() ?? "",
            Avatar = player.GetProperty("avatarfull").GetString() ?? "",
            ProfileUrl = player.GetProperty("profileurl").GetString() ?? ""
        };
    }
}