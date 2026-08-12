using Microsoft.EntityFrameworkCore;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;

namespace ProjetoZ.Api.Services;

// Roda em segundo plano, checando periodicamente se a semana virou. Quando
// vira, decide o vencedor (clipe mais curtido de cada usuário, e entre os
// usuários o maior desses valores — empate resolvido por sorteio), credita
// 500 coins, registra a compra e apaga todos os clipes da semana.
public class FechamentoSemanalClipesService : BackgroundService
{
    private static readonly TimeSpan IntervaloChecagem = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FechamentoSemanalClipesService> _logger;

    public FechamentoSemanalClipesService(IServiceScopeFactory scopeFactory, ILogger<FechamentoSemanalClipesService> logger)
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
                await VerificarEFecharSemanaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar/fechar a semana de clipes.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task VerificarEFecharSemanaAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var inicioSemanaAtual = Semana.InicioSemanaAtualUtc();

        var config = await context.ClipeConfigs.FirstOrDefaultAsync(ct);

        if (config == null)
        {
            // Primeira execução: só define a marca d'água, sem fechar nada
            // (não existe "semana anterior" real na primeira vez).
            context.ClipeConfigs.Add(new ClipeConfig { Id = 1, UltimoFechamento = inicioSemanaAtual });
            await context.SaveChangesAsync(ct);
            return;
        }

        if (config.UltimoFechamento >= inicioSemanaAtual)
            return;

        var clipes = await context.Clipes.ToListAsync(ct);

        if (clipes.Count > 0)
        {
            var curtidas = await context.ClipeCurtidas.ToListAsync(ct);
            var curtidasPorClipe = curtidas
                .GroupBy(c => c.ClipeId)
                .ToDictionary(g => g.Key, g => g.Count());

            var melhorPorUsuario = clipes
                .GroupBy(c => c.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MelhorClipe = g.OrderByDescending(c => curtidasPorClipe.GetValueOrDefault(c.Id, 0)).First(),
                    MaxCurtidas = g.Max(c => curtidasPorClipe.GetValueOrDefault(c.Id, 0))
                })
                .ToList();

            var maiorPontuacao = melhorPorUsuario.Max(u => u.MaxCurtidas);

            if (maiorPontuacao > 0)
            {
                var empatados = melhorPorUsuario.Where(u => u.MaxCurtidas == maiorPontuacao).ToList();
                var vencedor = empatados[Random.Shared.Next(empatados.Count)];

                var usuarioVencedor = await context.Users.FindAsync([vencedor.UserId], ct);

                if (usuarioVencedor != null)
                {
                    usuarioVencedor.Coins += 500;

                    context.Compras.Add(new Compra
                    {
                        Id = Guid.NewGuid(),
                        UserId = usuarioVencedor.Id,
                        Tipo = "clipe",
                        Descricao = empatados.Count > 1
                            ? $"Clipe da semana (desempate por sorteio): {vencedor.MelhorClipe.Titulo}"
                            : $"Clipe da semana: {vencedor.MelhorClipe.Titulo}",
                        Coins = 500,
                    });
                }
            }

            context.ClipeCurtidas.RemoveRange(curtidas);
            context.Clipes.RemoveRange(clipes);
        }

        config.UltimoFechamento = inicioSemanaAtual;

        await context.SaveChangesAsync(ct);
    }
}
