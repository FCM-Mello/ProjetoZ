# Entrega — API de Grupos e Ranking

Resposta ao `API_Grupo_Ranking.md`. Tudo abaixo está implementado, testado (166 testes automatizados + verificação manual contra Postgres real) e no ar assim que o deploy rodar. Base URL continua `https://arkz.dev.br/api/`.

## Resumo rápido

| Item pedido | Status |
|---|---|
| `POST /api/game/ranking/kd` com `zumbiKills`, `kothCompletados`, `segundosJogados` | ✅ Implementado exatamente como pedido |
| `POST /api/game/ranking/koth` | Sem mudança (já existia) |
| `POST /api/game/grupos/adicionar` + `POST /api/game/grupos/expulsar` | ✅ Implementado — **substitui o `POST /grupos/sync` original, ver nota abaixo** |
| `POST /api/game/ranking/jogador` | ✅ Implementado — **ficou `POST` com `apiKey`/`steamId` no corpo, não `GET` com chave no header** (ver nota abaixo) |
| `POST /api/game/grupos/jogador` | ✅ Implementado — mesma mudança acima |

Dois ajustes em relação ao documento original:
1. Os dois endpoints de leitura de 1 jogador viraram `POST` com corpo JSON em vez de `GET` com a chave no header `X-Api-Key`. Mantém o cliente HTTP do mod num único padrão de chamada (sempre `POST` + corpo) em toda a API.
2. O `POST /grupos/sync` (lote, substitui tudo a cada chamada) foi **trocado por dois endpoints incrementais**: `grupos/adicionar` (1 jogador entra num clã) e `grupos/expulsar` (1 jogador sai/é expulso). Além disso, **clã agora só é criado pelo site** — `grupos/adicionar` nunca cria um clã novo, só adiciona jogador a um que já existe (`404` se não existir), e todo clã tem um **limite de 6 membros contando o líder**.

## Única coisa que vale saber: Grupo agora é a mesma coisa que Clã do site

O site ganhou um sistema de clã (jogador cria pelo site, com nome/descrição/estandarte, aprova entrada, promove admin etc.). Em vez de manter isso como uma tabela separada, unificamos: **Grupo (o que o mod sincroniza) e Clã (o que o site gerencia) são a mesma entidade no banco.**

Como todo clã nasce no site, `grupos/adicionar` identifica o clã pelo Guid interno que o site já usa (o mesmo `id` que `grupos/jogador` devolve) — não existe mais conceito de "id de grupo do mod".

- `grupos/adicionar` só adiciona; nunca cria clã nem mexe em nome/líder (isso é 100% controlado pelo site). Membro que chama de novo pro mesmo jogador é idempotente. `400` se o clã já tiver 6 membros.
- `grupos/expulsar` remove o vínculo do jogador; se ele era o líder, promove automaticamente o próximo (admin mais antigo, senão membro comum mais antigo); se ninguém sobrar, o clã é apagado.

**Bônus pro mod, de graça**: como agora é a mesma tabela, o `grupos/jogador` também enxerga clãs criados pelo site. Se um jogador entrar num clã pelo site do ArkZ, a tela de grupo do jogo já mostra isso — sem precisar de nenhum código novo do lado do mod.

**Conflito de vínculo**: se por acaso um jogador estiver num clã do site e o mod chamar `grupos/adicionar` mostrando ele em outro grupo (ex: saiu do clã do site e entrou num grupo no jogo, ou vice-versa em algum fluxo futuro), **a informação do mod sempre vence** — o vínculo antigo é desfeito automaticamente, sem erro, sem exigir nada do mod.

## Contrato final dos endpoints

### `POST /api/game/ranking/kd`

```json
// request
{
  "apiKey": "...",
  "steamId": "76561198886359962",
  "kills": 42,
  "deaths": 7,
  "zumbiKills": 340,
  "kothCompletados": 4,
  "segundosJogados": 45230
}

// response 200 (corpo vazio)
```

- Todos os valores são **totais absolutos** (o mod manda o valor atual, não incrementa).
- `400` se algum valor vier negativo.
- `404` se `steamId` desconhecido da API.

### `POST /api/game/ranking/jogador`

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response 200
{
  "steamId": "76561198886359962",
  "nome": "Fulano",
  "kills": 42,
  "deaths": 7,
  "zumbiKills": 340,
  "kothCompletados": 5,
  "segundosJogados": 45230
}
```

- `404` se o jogador nunca sincronizou nenhum dado de ranking.

### `POST /api/game/grupos/adicionar`

Adiciona 1 jogador a 1 clã que **já existe** (criado pelo site).

```json
// request
{
  "apiKey": "...",
  "id": "3f1a9c2e-...",
  "steamId": "76561198886359962"
}

// response 200 (corpo vazio)
```

- `404` se `id` não corresponder a nenhum clã.
- `400` se o clã já tiver 6 membros (limite fixo, contando o líder).
- Entra sempre como membro comum — quem é líder/admin é decidido no site, não por esse endpoint.
- Jogador já vinculado a outro clã tem o vínculo antigo desfeito.
- Idempotente pro mesmo `steamId` já membro desse clã.

### `POST /api/game/grupos/expulsar`

Remove 1 jogador do grupo — chamar quando ele sai ou é expulso no jogo.

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response 200
{ "claApagado": false, "novoLiderSteamId": "76561198000000111" }
```

- `404` se `steamId` não está em nenhum grupo.
- Se não era o líder: só sai, `novoLiderSteamId` vem `null`.
- Se era o líder: promove o admin mais antigo, senão o membro comum mais antigo; sem mais ninguém, apaga o clã (`claApagado: true`).

### `POST /api/game/grupos/jogador`

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response 200 — tem grupo
{
  "temGrupo": true,
  "id": "3f1a9c2e-...",
  "nome": "Grupo de Fulano",
  "liderSteamId": "76561198886359962",
  "membros": ["76561198886359962", "76561198000000111"]
}

// response 200 — sem grupo
{ "temGrupo": false }
```

- "Sem grupo" é `200` com `temGrupo: false`, nunca `404`.
- `id` é o Guid interno do site (todo clã nasce lá) — é esse mesmo valor que vai no `id` de `grupos/adicionar`.

## Autenticação

Todo endpoint é `POST` com `apiKey` no corpo do JSON — inclusive os dois novos de leitura sob demanda. Chave errada ou ausente: `401`.
