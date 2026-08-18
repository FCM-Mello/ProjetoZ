# Sincronização automática de produtos com o servidor DayZ

Planejamento — servidor DayZ ainda não está hospedado, nada aqui foi implementado. Objetivo: sempre que um produto for criado, editado ou excluído no admin do site, o backend escreve automaticamente um JSON atualizado num servidor DayZ separado, pra um mod ler e saber quais itens existem, preço e ícone.

Isso é um conceito diferente do catálogo descrito em [`loja-mod.md`](loja-mod.md) — lá, os itens da "loja dentro do jogo" são mantidos manualmente do lado do mod, porque só existem lá. Aqui é o oposto: os produtos que **já existem no site** (tabela `Products`, admin em `/Admin` → aba de produtos) viram a fonte da verdade, e o mod passa a *ler* esse catálogo em vez de manter o próprio.

## Pré-requisitos (nada disso existe ainda)

1. **Servidor DayZ hospedado**, com IP/host acessível a partir da VPS do site (216.238.107.240) — porta SSH/SFTP liberada no firewall dos dois lados.
2. **Par de chaves SSH dedicado** só pra essa sincronização (não reaproveitar a chave de deploy do site):
   - Gerar local: `ssh-keygen -t ed25519 -f dayz-sync-key -C "arkz-produtos-sync"` (sem senha, já que vai rodar sem interação).
   - Chave pública (`dayz-sync-key.pub`) instalada em `~/.ssh/authorized_keys` do usuário certo no servidor DayZ.
   - Chave privada guardada como variável de ambiente no `.env` da VPS do site (mesmo padrão do `JWT_KEY`/`GAMESERVER_API_KEY` — nunca commitada), ex: `DAYZ_SYNC_SSH_KEY`.
3. **Caminho exato no servidor DayZ** onde o JSON deve ser escrito (ex: `/dayz/mpmissions/chernarusplus/db/produtos_site.json` — depende de onde o mod vai procurar o arquivo).
4. **Pasta de `.paa`** e a convenção de nome de arquivo — combinar com o mod: provavelmente `{produtoId}.paa` ou um slug do nome, pra bater com o campo `imagem` do JSON. Essa parte continua **manual** por enquanto (ver seção "Fora do escopo" abaixo).

## Lacuna nos dados: falta um "classname" do DayZ

O `Product` do site (`backend/ProjetoZ.Domain/Entities/Product.cs`) hoje tem `Nome`, `Preco`, `Imagem`, `Descricao`, `Estoque`, `Categoria` — nenhum campo diz **qual item real do jogo** (classname do Enfusion, tipo `M4A1` ou `TacticalJacket_Black`) aquele produto representa. Sem isso, o mod recebe nome/preço/ícone bonitos mas não sabe o que efetivamente dar/spawnar pro jogador.

Precisa de um campo novo, ex: `Product.DayzClassName` (string, opcional — nem todo produto precisa necessariamente ser algo entregável in-game, ex: um pacote que já é resolvido só pelo `Inventario` do site). Isso implica:
- Migration nova em `ProjetoZ.Persistence`.
- Campo novo no formulário de criar/editar produto no admin (`Admin/page.tsx` ou onde for o modal de produto).
- Validação: ideal ter uma lista de classnames válidos pra evitar erro de digitação, mas isso exigiria importar a lista de tipos do DayZ (fora de escopo por ora — começar como texto livre).

## Formato do JSON proposto

```json
{
  "geradoEm": "2026-08-18T03:00:00Z",
  "produtos": [
    {
      "id": "019fe9c2-...",
      "nome": "Ferrari",
      "preco": 10,
      "descricao": "carro",
      "categoria": "carros",
      "classname": "OffroadHatchback_Blue",
      "imagem": "produtos/019fe9c2.paa",
      "estoque": -1
    }
  ]
}
```

Pontos em aberto:
- `estoque: -1` pra "ilimitado"? Precisa confirmar convenção com o que o mod espera.
- `imagem` é só o nome do arquivo (relativo à pasta de `.paa` do passo 4 acima) ou precisa do caminho completo? Depende de como o mod carrega textura.
- Reenviar o catálogo **inteiro** a cada mudança (mais simples, sempre consistente) vs. enviar só o diff — começar pelo catálogo inteiro, é mais simples e o arquivo deve ser pequeno.

## Onde entra no backend

- Trigger: nas ações de `ProductsController` que já existem — criar, editar, excluir produto. Depois de persistir no Postgres, chamar um `DayzSyncService.SincronizarAsync()` que busca todos os produtos e sobe o JSON via SFTP.
- Biblioteca: `SSH.NET` (pacote NuGet `Renci.SshNet`) — cliente SFTP maduro pra .NET, mesmo em Linux (container da API já roda em Linux).
- **Não bloquear a resposta do admin** por causa de uma dependência externa (o servidor DayZ pode estar offline, reiniciando, etc.): disparar a sincronização em background (`Task.Run` ou um `IHostedService`/fila simples) e logar erro se falhar, em vez de fazer o `POST /api/admin/produtos` esperar a resposta do SFTP.
- Fallback manual: talvez valha um botão "Ressincronizar" no admin, pra re-disparar sem precisar editar um produto de novo, caso a sincronização tenha falhado quando o servidor DayZ estava fora do ar.

## Fora de escopo por agora

- **Conversão automática de `.paa`**: formato proprietário da Bohemia/Enfusion, sem biblioteca padrão em .NET/Node/Python. As ferramentas existentes (`ImageToPAA`, Bank Tools do Mikero) são Windows-only. Por enquanto, o `.paa` de cada produto continua sendo gerado e copiado manualmente pra pasta certa no servidor DayZ — só o JSON (nome, preço, classname) é automático.
- **Validação de `classname` contra a lista real de tipos do DayZ** — texto livre por enquanto.
- **UI da loja dentro do mod** consumindo esse JSON — isso já está descrito em [`loja-mod.md`](loja-mod.md), que precisa ser revisto pra ler esse catálogo novo em vez do catálogo estático que descrevia antes.

## Resumo do que falta pra começar a implementar

- [ ] Servidor DayZ hospedado e acessível por SSH a partir da VPS do site
- [ ] Par de chaves SSH gerado e instalado
- [ ] Confirmar caminho remoto do JSON e da pasta de `.paa` com quem administra o servidor
- [ ] Confirmar formato exato do JSON com o lado do mod (campos acima são proposta, não travado)
- [ ] Migration: `Product.DayzClassName`
- [ ] Campo novo no formulário de produto do admin
- [ ] `DayzSyncService` (SSH.NET) + disparo assíncrono nas ações de criar/editar/excluir do `ProductsController`
