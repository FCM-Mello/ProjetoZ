---
name: feature-sorteios
description: Sistema de sorteios/raffles — admin cria, usuários entram de graça, admin sorteia, prêmio (VIP e/ou produtos) é concedido automaticamente. Use ao mexer em qualquer coisa relacionada a sorteios.
---

# Sistema de sorteios

## Modelo de dados

- `backend/ProjetoZ.Domain/Entities/Sorteio.cs` — `Titulo`, `Descricao`, `PremioVipNivel` (int?, 1-3), `PremioProdutoIds` (`List<Guid>`), `Status` (`"aberto"` | `"sorteado"`), `VencedorUserId` (Guid?), `CriadoEm`, `SorteadoEm`.
- `backend/ProjetoZ.Domain/Entities/SorteioParticipante.cs` — join simples `SorteioId` + `UserId`.

## Endpoints (`backend/ProjetoZ.Api/Controllers/SorteiosController.cs`)

- `GET /api/sorteios` — `[Authorize]`, lista todos com contagem de participantes, se o usuário logado já participa, e nome do vencedor resolvido.
- `POST /api/sorteios` — `[Authorize(Policy = "Admin")]`, cria. Exige pelo menos um prêmio (VIP nível 1-3 e/ou 1+ produtos).
- `POST /api/sorteios/{id}/entrar` — `[Authorize]`, idempotente (entrar de novo não duplica), bloqueia se `Status != "aberto"`.
- `POST /api/sorteios/{id}/sortear` — `[Authorize(Policy = "Admin")]`, escolhe vencedor com `Random.Shared.Next(participantes.Count)`, concede o prêmio, marca `"sorteado"`.
- `DELETE /api/sorteios/{id}` — `[Authorize(Policy = "Admin")]`.

## Como o prêmio é concedido (dentro de `Sortear`)

```csharp
if (sorteio.PremioVipNivel.HasValue)
{
    ganhador.VipNivel = sorteio.PremioVipNivel.Value;
    ganhador.VipExpiraEm = DateTime.UtcNow.AddDays(VipTiers.DuracaoDias); // sempre 30 dias fixos
}

if (sorteio.PremioProdutoIds.Count > 0)
    ganhador.Inventario = [.. ganhador.Inventario, .. sorteio.PremioProdutoIds];
```

Registra uma `Compra` com `Tipo = "sorteio"`, `Coins = 0` (o prêmio não é em coins, é VIP/produto — o Histórico do frontend trata esse tipo mostrando "🏆 Prêmio ganho" em vez de valor de coins).

## Frontend

`frontend/app/Sorteios/page.tsx` + `components/SorteioModal.tsx` (form de criação, admin only). Botão "Sortear" e "Excluir" só aparecem se `user.isAdmin`.
