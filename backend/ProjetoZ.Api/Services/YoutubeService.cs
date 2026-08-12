using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjetoZ.Api.Services;

public class YoutubeService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public YoutubeService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    // Usa o access token da própria conta Google do usuário (obtido no
    // vínculo via OAuth) pra descobrir o canal dele.
    public async Task<(string Id, string Nome)?> ObterCanalAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var items = json.RootElement.GetProperty("items");

        if (items.GetArrayLength() == 0)
            return null;

        var canal = items[0];
        var id = canal.GetProperty("id").GetString() ?? "";
        var nome = canal.GetProperty("snippet").GetProperty("title").GetString() ?? "";

        return (id, nome);
    }

    // Usa a chave de API do servidor (dados públicos, não precisa do token
    // de ninguém) pra descobrir de qual canal um vídeo específico é e
    // quando ele foi publicado (pra impedir postar clipe antigo).
    public async Task<(string ChannelId, DateTime PublicadoEm)?> ObterInfoDoVideoAsync(string videoId)
    {
        var apiKey = _configuration["Google:YoutubeApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            return null;

        var response = await _http.GetAsync(
            $"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={Uri.EscapeDataString(videoId)}&key={Uri.EscapeDataString(apiKey)}");

        if (!response.IsSuccessStatusCode)
            return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var items = json.RootElement.GetProperty("items");

        if (items.GetArrayLength() == 0)
            return null;

        var snippet = items[0].GetProperty("snippet");
        var channelId = snippet.GetProperty("channelId").GetString();
        var publicadoEm = snippet.GetProperty("publishedAt").GetDateTime();

        if (channelId == null)
            return null;

        return (channelId, publicadoEm);
    }

    public static string? ExtrairVideoId(string url)
    {
        var match = Regex.Match(
            url,
            @"(?:youtube\.com\/(?:watch\?v=|embed\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");

        return match.Success ? match.Groups[1].Value : null;
    }
}
