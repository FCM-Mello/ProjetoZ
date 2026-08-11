# Loja de itens do mod (tela in-game)

Especificação de layout para a tela de loja dentro do servidor DayZ (mod), consumindo a mesma API do site ArkZ. Objetivo: o jogador vê seu saldo de Az Coins, o inventário do que já comprou, e uma loja de itens exclusivos do mod pra gastar esses coins — visualmente alinhada ao site.

## Fontes de dado

| Dado | Origem | Observação |
|---|---|---|
| Saldo de coins | Nossa API (`/api/game/player`) | Já existe |
| Inventário do jogador | Nossa API (`/api/game/player`) | Já existe — só produtos comprados no site |
| Catálogo da loja do mod | JSON local do mod | Novo — não existe na nossa API |
| Registrar compra / debitar coins | Nossa API (`/api/game/comprar`) | **Implementado** |

### 1. Status do jogador — `POST /api/game/player`

```json
// request
{ "apiKey": "...", "steamId": "76561198886359962" }

// response (PlayerStatusDto)
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

Chamado ao abrir a tela (pra popular coins + inventário) e de novo depois de cada compra (pra atualizar o saldo).

### 2. Catálogo da loja do mod — JSON estático

Fica do lado do mod porque são itens que só existem no jogo (o site não tem como saber a classe/textura deles). Sugestão de formato:

```json
[
  {
    "id": "jaqueta_militar",
    "nome": "Jaqueta Militar",
    "preco": 150,
    "imagem": "loja/jaqueta_militar.png",
    "categoria": "roupas"
  },
  {
    "id": "kit_medico_avancado",
    "nome": "Kit Médico Avançado",
    "preco": 80,
    "imagem": "loja/kit_medico.png",
    "categoria": "consumiveis"
  }
]
```

`imagem` pode ser um caminho local (asset do mod) ou uma URL — depende de como a UI do mod carrega textura.

### 3. Compra de item — `POST /api/game/comprar` (implementado)

Segue o mesmo modelo de autenticação server-to-server do `/api/game/player` (chave secreta no corpo, sem JWT — quem chama é o servidor do mod, não o jogador):

```json
// request
{
  "apiKey": "...",
  "steamId": "76561198886359962",
  "itemId": "jaqueta_militar",
  "itemNome": "Jaqueta Militar",
  "preco": 150
}

// response
{ "coins": 750 }
```

Regras:
- Valida `apiKey` (mesmo padrão de `ValidarApiKey` já existente).
- Busca o usuário pelo `steamId`; 404 se não achar.
- 400 se `itemId` vazio ou `preco <= 0`.
- 400 se `coins` do usuário for menor que `preco`.
- Debita `preco` de `user.Coins`.
- Registra em `Compras` com `Tipo = "mod"` e `Descricao = itemNome` — assim a compra feita **dentro do jogo** aparece no [Histórico](../frontend/app/Historico/page.tsx) do site também, no mesmo lugar que compras feitas pelo site (rotulada como "In-game").
- Como esses itens não existem na tabela `Products`, essa compra **não** entra no `Inventario` do usuário (que é uma lista de `Guid` de produtos) — o item comprado passa a existir só dentro do jogo, controlado pelo mod.

## Layout da tela

Referência visual: paleta e tipografia do site ([`globals.css`](../frontend/app/globals.css)).

```
┌────────────────────────────────────────────────────────────┐
│  LOJA DO SERVIDOR                              🪙 900       │  ← header
├───────────────────────┬────────────────────────────────────┤
│  [ LOJA ]  MEU INVENTÁRIO                                   │  ← abas
├───────────────────────┴────────────────────────────────────┤
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐             │
│  │ imagem │  │ imagem │  │ imagem │  │ imagem │             │
│  │ Nome   │  │ Nome   │  │ Nome   │  │ Nome   │             │
│  │ 🪙 150 │  │ 🪙 80  │  │ 🪙 300 │  │ 🪙 50  │             │
│  │[COMPRAR]│  │[COMPRAR]│  │[COMPRAR]│  │[COMPRAR]│             │
│  └────────┘  └────────┘  └────────┘  └────────┘             │
│  ┌────────┐  ┌────────┐  ...                                │
└────────────────────────────────────────────────────────────┘
```

### Header
- Pílula de coins no canto direito, igual ao `.coinBadge` do site: fundo `rgba(255,209,102,.08)`, borda `rgba(255,209,102,.3)`, texto cor `--color-coin` (`#ffd166`), ícone 🪙 + valor.
- Título "LOJA DO SERVIDOR" à esquerda, maiúsculo, letter-spacing largo (mesmo tratamento de `h2`/`.section-title` do site).

### Abas: Loja / Meu inventário
Duas abas simples no topo do conteúdo, mesma linguagem visual dos botões de categoria da Home (`.category-chip`): fundo `--color-surface-alt`, borda `--color-border-strong`, aba ativa com fundo `--color-accent-soft` e texto `--color-accent-strong`.

### Aba "Loja"
Grade de cards (mesmo padrão dos cards de produto da Home / pacotes do Az):
- Imagem do item no topo (vem do JSON do mod).
- Nome do item.
- Preço com ícone 🪙, cor `--color-coin`.
- Botão "Comprar" (`--color-accent` de fundo, texto escuro `#1a1004`), **desabilitado e acinzentado** se `coins < preco`.
- Ao clicar: chama `POST /api/game/comprar`, mostra estado de carregamento no botão ("Comprando..."), atualiza o saldo no header ao concluir. Erro (saldo insuficiente, item indisponível) aparece como aviso inline, mesmo padrão do `.vipAviso-erro` do site.

### Aba "Meu inventário"
Grade somente-leitura dos itens do jogador (`inventario[]` retornado por `/api/game/player`):
- Mesmo visual dos slots da página [Inventário](../frontend/app/Inventario/page.tsx) do site: imagem, nome, badge de quantidade no canto se `quantidade > 1`.
- Sem botão de ação — é só consulta.

### Paleta usada

| Token | Valor | Uso |
|---|---|---|
| `--color-bg` | `#0a0c10` | fundo da tela |
| `--color-surface` | `#1a1e24` | fundo dos cards |
| `--color-border` | `rgba(255,255,255,.08)` | bordas |
| `--color-accent` | `#ff9f1c` | botão de compra, destaques |
| `--color-coin` | `#ffd166` | preço, saldo de coins |
| `--color-text` | `#eef2f5` | texto principal |
| `--color-text-muted` | `#8b93a1` | texto secundário |
| `--color-danger` | `#ef4444` | erros (saldo insuficiente) |

## Resumo do que falta implementar

- [x] Endpoint `POST /api/game/comprar` no `GameController` (débito de coins + registro em `Compras` com `Tipo = "mod"`)
- [x] Ajustar `Historico/page.tsx` do site pra rotular `Tipo = "mod"` como "In-game"
- [ ] JSON do catálogo de itens (do lado do mod)
- [ ] Tela em si, dentro do mod (Workbench / Enforce Script), seguindo este layout
