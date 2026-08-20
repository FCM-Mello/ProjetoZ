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
2. O `POST /grupos/sync` (lote, substitui tudo a cada chamada) foi **trocado por dois endpoints incrementais**: `grupos/adicionar` (1 jogador entra/cria grupo) e `grupos/expulsar` (1 jogador sai/é expulso). O sync em lote batia num índice único de nome de clã que não tinha exceção pra grupo do mod — dois grupos com nome igual (ou ambos sem nome) derrubavam a chamada inteira com `500`. O modelo incremental evita esse cenário de origem e também é mais simples de disparar direto dos eventos do jogo (entrar/sair de grupo), em vez de acumular estado pra mandar em lote a cada 15min.

## Única coisa que vale saber: Grupo agora é a mesma coisa que Clã do site

O site ganhou um sistema de clã (jogador cria pelo site, com nome/descrição/estandarte, aprova entrada, promove admin etc.). Em vez de manter isso como uma tabela separada, unificamos: **Grupo (o que o mod sincroniza) e Clã (o que o site gerencia) são a mesma entidade no banco.**

Isso afeta `grupos/adicionar` e `grupos/expulsar`: os dois mexem no clã via `id` de grupo (`GrupoModId`), e clãs sem esse `id` (criados no site) nunca são tocados por eles.

- `grupos/adicionar` faz **upsert por `id`**: grupo que já existe é atualizado (nome, líder); grupo novo é criado na primeira chamada. Membro que chama de novo pro mesmo jogador é idempotente.
- `grupos/expulsar` remove o vínculo do jogador; se ele era o líder, promove automaticamente o próximo (admin mais antigo, senão membro comum mais antigo); se ninguém sobrar, o clã é apagado — dissolução agora só acontece por aqui, não existe mais "sumiu de um lote = removido".

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

Adiciona 1 jogador a 1 grupo — cria o clã na primeira chamada com esse `id`.

```json
// request
{
  "apiKey": "...",
  "id": "1755500000-482913",
  "nome": "Grupo de Fulano",
  "liderSteamId": "76561198886359962",
  "steamId": "76561198886359962"
}

// response 200 (corpo vazio)
```

- `steamId == liderSteamId` vira admin automaticamente.
- Jogador já vinculado a outro clã (site ou mod) tem o vínculo antigo desfeito.
- Idempotente pro mesmo `steamId` já membro desse clã.
- Nome de grupo não precisa ser único entre si (só nomes de clã criados no site precisam).

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

// response 200 — tem grupo (de origem mod OU site)
{
  "temGrupo": true,
  "id": "1755500000-482913",
  "nome": "Grupo de Fulano",
  "liderSteamId": "76561198886359962",
  "membros": ["76561198886359962", "76561198000000111"]
}

// response 200 — sem grupo
{ "temGrupo": false }
```

- "Sem grupo" é `200` com `temGrupo: false`, nunca `404`.
- Se o grupo for um clã criado pelo site (sem `id` de mod), o `id` devolvido aqui é o identificador interno do site — ainda assim útil pra exibir/correlacionar, só não é um id que o mod reconhece de volta.

## Autenticação

Todo endpoint é `POST` com `apiKey` no corpo do JSON — inclusive os dois novos de leitura sob demanda. Chave errada ou ausente: `401`.
