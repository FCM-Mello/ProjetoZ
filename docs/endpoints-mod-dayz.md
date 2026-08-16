# Endpoints da API pro mod DayZ (GameController)

Referência de tudo que o servidor de jogo (mod ArkZ) pode chamar na API do site. Todos os endpoints ficam em `backend/ProjetoZ.Api/Controllers/GameController.cs`, sob a rota base `/api/game`.

## Autenticação

Não usa JWT — o servidor de jogo não é um usuário logado no site. Todo endpoint recebe `apiKey` **no corpo** da requisição (não em header) e valida contra `GameServer:ApiKey` (env var `GAMESERVER_API_KEY`) com comparação resistente a timing attack.

- Chave errada ou ausente → `401 Unauthorized`.
- Todas as chamadas são `POST` com corpo JSON, mesmo as que só leem dado (convenção do controller, não RESTful "puro" — é assim porque o cliente HTTP do mod manda tudo no corpo).

## `POST /api/game/player`

Status completo de um jogador: VIP efetivo, coins, inventário de produtos comprados no site.

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response 200
{
  "steamId": "76561198886359962",
  "vip": true,
  "vipNivel": 2,
  "vipNivelNome": "Prata",
  "vipExpiraEm": "2026-09-10T00:00:00Z",
  "coins": 900,
  "inventario": [
    { "produtoId": "019fe9c2-...", "nome": "Ferrari", "quantidade": 1 }
  ]
}
```

- `404` se `steamId` não corresponde a nenhum usuário cadastrado no site.
- `vipNivel`/`vip` já consideram expiração (`VipTiers.NivelEfetivo`) — não precisa checar `vipExpiraEm` manualmente.
- `inventario` é só produtos da loja do site (tabela `Products`); itens comprados via `/comprar` (abaixo) não entram aqui, porque só existem dentro do jogo.

## `POST /api/game/comprar`

Debita coins de um jogador ao comprar um item que só existe no jogo (não cadastrado na tabela `Products` do site — quem decide preço/catálogo é o mod).

```json
// request
{
  "apiKey": "...",
  "steamId": "76561198886359962",
  "itemId": "jaqueta_militar",
  "itemNome": "Jaqueta Militar",
  "preco": 150
}

// response 200
{ "coins": 750 }
```

- `preco` é **decidido pelo mod** (server-side) — o site nunca valida contra uma lista fixa, só confia no valor recebido. Não confiar em preço vindo do cliente do jogo.
- `400` se `itemId` vazio ou `preco <= 0`.
- `400 "Saldo de Az Coins insuficiente."` se `coins` do usuário for menor que `preco`.
- `404` se `steamId` não corresponde a nenhum usuário.
- Débito é atômico (`UPDATE ... WHERE Coins >= preco`) — duas compras simultâneas do mesmo jogador não geram saldo negativo.
- Fica registrado em `Compras` com `Tipo = "mod"` — aparece no [Histórico](../frontend/app/Historico/page.tsx) do site rotulado como "In-game".
- `itemId` não passa por nenhuma validação contra catálogo — qualquer string não-vazia é aceita (inclusive um `itemId` que o site nunca viu antes, como `"resgate_expresso"`).

## `POST /api/game/vips`

Lista `steamId` + nível de todo jogador com VIP ativo — pro mod sincronizar benefícios em lote em vez de consultar jogador por jogador.

```json
// request
{ "apiKey": "..." }

// response 200
[
  { "steamId": "76561198886359962", "vipNivel": 2 },
  { "steamId": "76561198000000001", "vipNivel": 1 }
]
```

- Filtra pelo campo bruto `VipNivel != 0` — **não considera expiração**. Um VIP vencido só some dessa lista depois que o job horário (`ExpiracaoVipService`) zerar o campo no banco (atraso máximo ~1h). Se precisar do valor já considerando expiração, usar `/api/game/player` por jogador.

## Sistema de seguro de veículos

Seguro dura **1 mês** a partir da criação. Cobre normalmente veículos, mas o `id`/`ItemId` é uma string livre do catálogo do mod (`ArkZ_Catalogo.c`) — não precisa ser sempre "carro".

### `POST /api/game/seguro`

Registra um seguro novo.

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962", "id": "carro" }

// response 200
{ "idSeguro": "0876959b-78e3-4386-81c3-66b9be1d0869" }
```

- `id` é o id do item no catálogo do mod (ex: `"carro"`), **não** confundir com `idSeguro` (gerado pelo site, identifica esse registro específico).
- Cada chamada cria um seguro novo — o mesmo jogador pode ter vários seguros do mesmo `id` (ex: 3 carros = 3 chamadas = 3 seguros independentes, cada um com seu próprio cooldown de resgate).
- Esse endpoint **não** recebe qual veículo específico está sendo segurado — só o tipo do item. O vínculo com um veículo concreto acontece depois, via `/veiculos/posicao` (abaixo).
- `400` se `id` vazio. `404` se `steamId` desconhecido.

### `POST /api/game/seguros`

Lista os seguros **ativos** (não expirados) de um jogador, com o estado do cooldown de resgate de cada um.

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response 200
[
  {
    "idSeguro": "0876959b-...",
    "id": "carro",
    "podeResgatar": false,
    "proximoResgateEm": "2026-08-18T04:59:23Z"
  }
]
```

- Seguro expirado (`ExpiraEm` no passado) **não aparece nessa lista**.
- `podeResgatar: true` → `proximoResgateEm` vem `null`.
- Cooldown de resgate é de **48h** desde o último resgate (`UltimoResgate == null` conta como liberado).

### `POST /api/game/seguro/resgate`

Marca um seguro como resgatado agora.

```json
// request (resgate normal, respeitando cooldown)
{ "apiKey": "...", "steamId": "76561198886359962", "idSeguro": "0876959b-..." }

// request (resgate expresso, pula cooldown)
{ "apiKey": "...", "steamId": "76561198886359962", "idSeguro": "0876959b-...", "pago": true }

// response 200 (nos dois casos)
{ "proximoResgateEm": "2026-08-18T04:59:23Z" }
```

- `pago` ausente ou `false` → comportamento normal: `400` se ainda dentro das 48h de cooldown.
- `pago: true` → **pula a checagem de cooldown** e resgata na hora. Só o servidor do mod deve mandar isso — normalmente depois de já ter debitado 500 coins via `/api/game/comprar` com `itemId: "resgate_expresso"` (ver fluxo completo abaixo).
- `400 "Esse seguro expirou."` em qualquer um dos dois casos se `ExpiraEm` já passou — resgate expresso **não** revive um seguro vencido.
- `404` se `idSeguro` não existe ou não pertence ao `steamId` informado.
- Nos dois casos, `UltimoResgate` é atualizado pra agora — o próximo resgate grátis (sem pagar) só libera depois de mais 48h a partir desse resgate, mesmo que ele tenha sido o expresso.

**Fluxo recomendado do resgate expresso** (mod já implementa assim):
1. Jogador clica "Resgatar agora — 500 AZCoins".
2. Mod chama `/api/game/comprar` com `{itemId: "resgate_expresso", itemNome: "Resgate Expresso", preco: 500}`. Se der `400` (saldo insuficiente), para aqui — nunca chama o resgate.
3. Só se o débito for confirmado (`200`), o mod chama `/api/game/seguro/resgate` com `pago: true`.
4. **Limitação conhecida**: os dois calls não são atômicos entre si. Se o passo 3 falhar depois do passo 2 ter debitado (ex: `idSeguro` inválido), o jogador perde os 500 coins sem ganhar o resgate. Na prática isso não deveria acontecer, porque o `idSeguro` usado no passo 3 vem de uma lista (`/api/game/seguros`) que o próprio site forneceu antes.

### `POST /api/game/veiculos/posicao`

Sincronização em lote da posição de todos os veículos segurados de todos os jogadores — pensado pra rodar como job periódico do mod (ex: a cada ~15min), numa única chamada, não uma por jogador.

```json
// request
{
  "apiKey": "...",
  "veiculos": [
    {
      "carroId": "2011680-906255",
      "steamId": "76561198886359962",
      "nome": "Land Rover",
      "posicaoGrid": "045 112",
      "x": 4523.1,
      "z": 11890.4
    }
  ]
}

// response 200
{ "atualizados": 1 }
```

- `carroId` é o identificador do veículo específico no mundo do jogo (ex: net id persistente) — **não** é `idSeguro` nem o `id`/`ItemId` do catálogo.
- **Vínculo automático**: o site não sabe de antemão qual `carroId` corresponde a qual seguro (o `/api/game/seguro` não recebe essa informação). Na primeira vez que um `carroId` aparece:
  - Se já existe um seguro desse jogador com esse `carroId` vinculado (de uma sincronização anterior), só atualiza a posição.
  - Senão, vincula ao **seguro mais antigo ainda sem `carroId`** desse jogador (`ExpiraEm` no futuro). Isso assume que o jogador segura os carros na mesma ordem que o mod começa a reportar a posição deles — se isso não bater com a realidade do mod, é preciso ajustar (ex: mandar o `carroId` já na criação do seguro).
  - Se não houver seguro disponível pra vincular (jogador sem seguro de veículo ativo, ou todos os seguros dele já vinculados a outros carros), a entrada é **ignorada silenciosamente** — não gera erro, só não conta em `atualizados`.
- Entradas com `steamId` desconhecido também são ignoradas silenciosamente — o resto do lote continua processando normalmente.
- `atualizados` no response conta quantos itens do lote resultaram em atualização real (vínculo novo ou posição atualizada em seguro já vinculado).
- A posição sincronizada aparece pro jogador na página `/Seguros` do site, plotada sobre o mapa do Chernarus+.

## Resumo dos endpoints

| Endpoint | Uso |
|---|---|
| `POST /api/game/player` | Status do jogador (VIP, coins, inventário) |
| `POST /api/game/comprar` | Debitar coins por item só-do-jogo |
| `POST /api/game/vips` | Lista em lote de quem é VIP (campo bruto, sem expiração) |
| `POST /api/game/seguro` | Criar seguro (1 mês) |
| `POST /api/game/seguros` | Listar seguros ativos + estado do cooldown |
| `POST /api/game/seguro/resgate` | Resgatar (normal ou expresso, `pago: true`) |
| `POST /api/game/veiculos/posicao` | Sync em lote de posição de veículos segurados |
