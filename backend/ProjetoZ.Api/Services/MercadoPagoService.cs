using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoZ.Api.Services;

public class MercadoPagoService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MercadoPagoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        _httpClient.BaseAddress = new Uri("https://api.mercadopago.com/");
    }

    private string AccessToken =>
        _configuration["MercadoPago:AccessToken"]
        ?? throw new InvalidOperationException(
            "MercadoPago:AccessToken não configurado. Defina via 'dotnet user-secrets' (dev) ou variável de ambiente MercadoPago__AccessToken (produção).");

    public async Task<MercadoPagoPreference> CriarPreferenciaAsync(
        Guid pedidoId, string titulo, decimal valor, string frontendBaseUrl, string webhookUrl)
    {
        var body = new
        {
            items = new[]
            {
                new
                {
                    title = titulo,
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = valor
                }
            },
            external_reference = pedidoId.ToString(),
            notification_url = webhookUrl,
            // auto_return exige um back_url.success https em domínio público — em dev
            // local (http://projetoz.local) o Mercado Pago rejeita a preferência.
            // Sem auto_return, o usuário só precisa clicar em "Voltar ao site" na
            // página de resultado do MP; os back_urls continuam funcionando normalmente.
            back_urls = new
            {
                success = $"{frontendBaseUrl}/Az?status=success",
                pending = $"{frontendBaseUrl}/Az?status=pending",
                failure = $"{frontendBaseUrl}/Az?status=failure"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Erro ao criar preferência no Mercado Pago: {json}");

        return JsonSerializer.Deserialize<MercadoPagoPreference>(json, JsonOptions)
            ?? throw new InvalidOperationException("Resposta inválida do Mercado Pago ao criar preferência.");
    }

    public async Task<MercadoPagoPayment?> ConsultarPagamentoAsync(string paymentId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<MercadoPagoPayment>(json, JsonOptions);
    }
}

public class MercadoPagoPreference
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("init_point")]
    public string InitPoint { get; set; } = string.Empty;
}

public class MercadoPagoPayment
{
    public long Id { get; set; }

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("external_reference")]
    public string? ExternalReference { get; set; }
}
