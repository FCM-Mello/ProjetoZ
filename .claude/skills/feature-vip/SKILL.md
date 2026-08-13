---
name: feature-vip
description: Sistema de VIP (3 níveis, 30 dias, compra com Az Coins). Use ao mexer em qualquer coisa relacionada a VIP — preços, duração, expiração, compra, painel de admin ou integração com o mod do jogo.
---

# Sistema de VIP

VIP é um atributo do usuário (não um produto na tabela `Products`). 3 níveis fixos: Bronze (1), Prata (2), Ouro (3), sempre 30 dias de duração.

## Modelo de dados

`User` (`backend/ProjetoZ.Domain/Entities/User.cs`):
- `VipNivel` (int, 0 = sem VIP)
- `VipExpiraEm` (DateTime?, null = sem VIP)

**O campo `VipNivel` não fica confiável sozinho.** Em quase todo lugar do site o "nível efetivo" é calculado na hora via `VipTiers.NivelEfetivo(nivel, expiraEm)` (retorna 0 se `expiraEm` já passou), sem alterar o valor salvo. Só o `ExpiracaoVipService` (roda a cada hora + uma vez ao subir) realmente zera o campo no banco quando vence. Qualquer código novo que leia `VipNivel` direto do banco sem passar por `NivelEfetivo` (como `GameController.GetVips`, que retorna o campo bruto por pedido explícito) pode mostrar VIP vencido há até 1h.

## Arquivos principais

- `backend/ProjetoZ.Api/Services/VipTiers.cs` — nomes, preços (Bronze 300 / Prata 600 / Ouro 1000 Az Coins), duração, `NivelEfetivo`, `NivelValido`.
- `backend/ProjetoZ.Api/Controllers/VipController.cs` — `GET /api/vip/niveis`, `POST /api/vip/comprar/{nivel}` (debita coins, seta `VipExpiraEm = agora + 30 dias`, sempre reseta a expiração pra 30 dias a partir da compra, não soma).
- `backend/ProjetoZ.Api/Services/ExpiracaoVipService.cs` — `BackgroundService`, zera `VipNivel`/`VipExpiraEm` de quem venceu, a cada 1h.
- `backend/ProjetoZ.Api/Controllers/AdminController.cs` — `POST usuarios/{id}/vip` (admin concede) e `DELETE usuarios/{id}/vip` (admin remove).
- `backend/ProjetoZ.Api/Controllers/GameController.cs` — `GetPlayer` inclui VIP efetivo (via `NivelEfetivo`); `GetVips` (`POST /api/game/vips`) retorna `{steamId, vipNivel}` de todo mundo com `VipNivel != 0` no campo bruto — usado pelo mod pra sincronizar em lote.
- Frontend: `frontend/app/Vip/page.tsx` (compra), `frontend/app/models/VipTier.ts`, `frontend/app/services/vipApi.ts`.

## Outras fontes de VIP

Além da compra direta, VIP também é concedido por: sorteio ganho (`SorteiosController.Sortear`, se o prêmio inclui VIP) e concessão manual de admin (`AdminController`). Todos os três caminhos setam `VipExpiraEm = DateTime.UtcNow.AddDays(VipTiers.DuracaoDias)` — sempre 30 dias fixos a partir do momento da concessão, nunca acumula com o que já tinha.
