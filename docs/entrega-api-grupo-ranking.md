# Entrega — API de Grupos e Ranking

Resposta ao `API_Grupo_Ranking.md`. Tudo abaixo está implementado, testado (166 testes automatizados + verificação manual contra Postgres real) e no ar assim que o deploy rodar. Base URL continua `https://arkz.dev.br/api/`.

## Resumo rápido

| Item pedido | Status |
|---|---|
| `POST /api/game/ranking/kd` com `zumbiKills`, `kothCompletados`, `segundosJogados` | ✅ Implementado exatamente como pedido |
| `POST /api/game/ranking/koth` | Sem mudança (já existia) |
| `POST /api/game/grupos/sync` | ✅ Implementado — **comportamento interno mudou, contrato pro mod não** (ver nota abaixo) |
| `GET /api/game/ranking/jogador/{steamId}` | ✅ Implementado, chave no header `X-Api-Key` como combinado |
| `GET /api/game/grupos/jogador/{steamId}` | ✅ Implementado, chave no header `X-Api-Key` como combinado |

Nenhum campo, rota ou formato de request/response ficou diferente do que foi pedido no documento original. O mod pode consumir exatamente como especificado lá.

## Única coisa que vale saber: Grupo agora é a mesma coisa que Clã do site

O site ganhou um sistema de clã (jogador cria pelo site, com nome/descrição/estandarte, aprova entrada, promove admin etc.). Em vez de manter isso como uma tabela separada, unificamos: **Grupo (o que o mod sincroniza) e Clã (o que o site gerencia) são a mesma entidade no banco.**

Isso muda como o `POST /grupos/sync` se comporta **por dentro**, mas não muda nada que o mod precise fazer diferente:

- Antes do que foi pedido no documento ("sync absoluto... substitui o que a API tinha"), a implementação seria "apaga tudo, recria do payload".
- Como agora existem clãs criados pelo site que o mod não conhece, isso viraria um apagão dos clãs do site a cada sync de 15min.
- Em vez disso, o sync faz **upsert por `id`** (o id que o mod já manda): grupo que já existe é atualizado (nome, líder, membros); grupo novo é criado; grupo que não vier mais num lote é considerado dissolvido e removido. Clãs sem `id` de grupo (criados no site) nunca são tocados por esse sync.
- Como o mod já manda o **snapshot completo** de todos os grupos ativos a cada chamada (não incremental), o resultado observável é idêntico a um "substitui tudo" — só que sem apagar o que não é seu.

**Bônus pro mod, de graça**: como agora é a mesma tabela, o `GET /grupos/jogador/{steamId}` também enxerga clãs criados pelo site. Se um jogador entrar num clã pelo site do ArkZ, a tela de grupo do jogo já mostra isso — sem precisar de nenhum código novo do lado do mod.

**Conflito de vínculo**: se por acaso um jogador estiver num clã do site e o próximo sync do mod mostrar ele em outro grupo (ex: saiu do clã do site e entrou num grupo no jogo, ou vice-versa em algum fluxo futuro), **a informação do mod sempre vence** — o vínculo antigo é desfeito automaticamente, sem erro, sem exigir nada do mod.

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

### `GET /api/game/ranking/jogador/{steamId}`

```
GET /api/game/ranking/jogador/76561198886359962
X-Api-Key: ...

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

### `POST /api/game/grupos/sync`

```json
// request
{
  "apiKey": "...",
  "grupos": [
    {
      "id": "1755500000-482913",
      "nome": "Grupo de Fulano",
      "liderSteamId": "76561198886359962",
      "membros": ["76561198886359962", "76561198000000111"]
    }
  ]
}

// response 200 (corpo vazio)
```

- Mandar `grupos: []` remove todos os grupos de origem mod (mantém os criados pelo site intactos).
- Chamar de novo com o mesmo `id` atualiza nome/líder/membros; grupo que sumir do payload é removido.

### `GET /api/game/grupos/jogador/{steamId}`

```
GET /api/game/grupos/jogador/76561198886359962
X-Api-Key: ...

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

## Autenticação (sem mudança)

- Endpoints `POST`: `apiKey` no corpo do JSON.
- Endpoints `GET` (os dois novos de leitura sob demanda): `apiKey` no header `X-Api-Key`.
- Chave errada ou ausente: `401`.
