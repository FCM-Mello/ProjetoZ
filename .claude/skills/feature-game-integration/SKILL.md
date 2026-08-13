---
name: feature-game-integration
description: Ponte servidor-a-servidor entre a API do site e o mod do servidor DayZ (GameController) — consulta de jogador por SteamID, compra de item do mod, lista de VIPs. Use ao mexer em qualquer coisa que o mod do jogo consome, ou ao adicionar um endpoint novo pra essa integração.
---

# Integração com o servidor de jogo (GameController)

`backend/ProjetoZ.Api/Controllers/GameController.cs` — endpoints chamados pelo mod do servidor DayZ, não por usuários logados no site.

## Autenticação: chave compartilhada, não JWT

O servidor de jogo não é um "usuário" com sessão — todo endpoint desse controller recebe `ApiKey` **no corpo** da requisição (não em header, pra se adequar ao HTTP client do mod) e valida com:

```csharp
CryptographicOperations.FixedTimeEquals(expected, provided)
```

Comparação resistente a timing attack — nunca trocar por `==`/`.Equals()` numa string de chave. A chave em si vem de `GameServer:ApiKey` (env var `GAMESERVER_API_KEY`).

## Endpoints

- `POST /api/game/player` (`PlayerLookupRequest: {ApiKey, SteamId}`) — devolve VIP efetivo (via `VipTiers.NivelEfetivo`, já considera expiração), coins, inventário (produtos comprados no site, agrupados com quantidade).
- `POST /api/game/comprar` (`PlayerComprarRequest: {ApiKey, SteamId, ItemId, ItemNome, Preco}`) — debita coins pra item que só existe no jogo (não cadastrado em `Products`). Registra `Compra` com `Tipo = "mod"`, que aparece no Histórico do site como "In-game". Débito via `ExecuteUpdateAsync` atômico (mesmo padrão dos outros controllers de compra, ver `feature-vip`).
- `POST /api/game/vips` (`ListaVipsRequest: {ApiKey}`) — devolve `{steamId, vipNivel}` de todo mundo com `VipNivel != 0` **no campo bruto**, sem considerar expiração (pedido explícito assim; o `ExpiracaoVipService` — ver `feature-vip` — mantém esse campo bruto sincronizado a cada hora, então o atraso máximo é de ~1h).

## Documentação relacionada

`docs/loja-mod.md` tem a especificação de layout de uma tela de loja dentro do jogo consumindo esses endpoints (proposta, não necessariamente implementada no mod ainda).

## Ao adicionar um endpoint novo aqui

Sempre `[ApiController]` sem `[Authorize]` de JWT (esse controller não usa `[Authorize]` nenhum — a autenticação é manual via `ValidarApiKey` no início de cada action), sempre primeira linha `if (!ValidarApiKey(request.ApiKey)) return Unauthorized();`. Se o endpoint muda dado do jogador (débito/crédito de coins), usar `ExecuteUpdateAsync` condicional em vez de ler-checar-salvar, pelo mesmo motivo do `feature-vip`.
