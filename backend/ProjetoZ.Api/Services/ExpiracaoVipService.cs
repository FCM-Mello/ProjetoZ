using Microsoft.EntityFrameworkCore;
using ProjetoZ.Persistence;

namespace ProjetoZ.Api.Services;

// Roda em segundo plano checando periodicamente se algum VIP venceu.
// O campo VipNivel não zera sozinho quando a data passa — em quase todo
// lugar do site isso é contornado calculando o "nível efetivo" na hora
// (VipTiers.NivelEfetivo), mas endpoints que leem o campo bruto (como
// GameController.GetVips) dependem desse serviço pra não mostrar VIP
// vencido há muito tempo.
public class ExpiracaoVipService : BackgroundService
{
    private static readonly TimeSpan IntervaloChecagem = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiracaoVipService> _logger;

    public ExpiracaoVipService(IServiceScopeFactory scopeFactory, ILogger<ExpiracaoVipService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloChecagem);

        do
        {
            try
            {
                await LimparVipsExpiradosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar VIPs expirados.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task LimparVipsExpiradosAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var agora = DateTime.UtcNow;

        var expirados = await context.Users
            .Where(u => u.VipNivel != 0 && u.VipExpiraEm != null && u.VipExpiraEm <= agora)
            .ToListAsync(ct);

        if (expirados.Count == 0)
            return;

        foreach (var user in expirados)
        {
            user.VipNivel = 0;
            user.VipExpiraEm = null;
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation("VIP zerado para {Quantidade} usuário(s) por expiração.", expirados.Count);
    }
}
