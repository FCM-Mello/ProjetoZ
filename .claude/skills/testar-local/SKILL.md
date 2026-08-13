---
name: testar-local
description: Fluxo completo pra testar mudanças de backend/frontend localmente via Docker antes de commitar — rebuild seguro (sem mascarar exit code), usuário sintético + JWT forjado, curl, limpeza. Use sempre que terminar de implementar algo e precisar validar antes de dar commit/push.
---

# Testar localmente via Docker

Ambiente local roda via `docker compose` (postgres, api, frontend, nginx). `http://localhost` já serve o site completo através do nginx (não precisa rodar `npm run dev`/`dotnet run` separados pra testar via navegador).

## 1. Rebuild — SEMPRE cheque o exit code de verdade

**Nunca** faça `docker compose build ... | tail -N` — isso mascara o exit code real (o pipe reporta o exit code do `tail`, não do build; um build que falhou aparenta ter tido sucesso). Já aconteceu de eu gastar vários ciclos testando contra uma imagem velha por causa disso.

```bash
docker compose build api frontend > /tmp/build.log 2>&1
echo "EXIT_CODE=$?"
tail -30 /tmp/build.log
```

Só prossiga se `EXIT_CODE=0` aparecer de verdade. Se não, leia o log inteiro (`cat /tmp/build.log`) antes de assumir qualquer coisa.

## 2. Trocar os containers — um serviço por vez, nunca em paralelo

```bash
docker compose ps                    # sempre cheque o estado atual antes de mexer
docker compose up -d api             # espera terminar
docker compose up -d frontend        # só depois, espera terminar
docker compose restart nginx         # sempre por último
```

Rodar comandos `docker compose` sobrepostos (ex: disparar `up -d frontend` de novo enquanto o anterior ainda está no meio de parar o container antigo) já travou o Docker Desktop várias vezes nesta sessão — container fica "zombie" e nem `docker kill` mata na hora. Se acontecer:

```bash
docker kill <nome-do-container>
docker rm -f <nome-do-container>
docker ps -a   # pode sobrar um container renomeado tipo "abc123_projetoz-frontend" — remove também
docker compose up -d <servico>
```

## 3. Criar usuário sintético + JWT pra testar autenticado

A chave de assinatura JWT local está em `backend/ProjetoZ.Api/appsettings.json` (`Jwt:Key`, atualmente `ProjetoZ_Super_Secret_Key_2026_08112005` — confirme no arquivo, pode mudar). Como é a mesma chave usada pela API rodando local, dá pra forjar um token válido sem passar pelo login real da Steam.

```bash
USER_ID=$(node -e "console.log(require('crypto').randomUUID())")
docker exec projetoz-postgres psql -U postgres -d projetoz -c "
INSERT INTO \"Users\" (\"Id\", \"Profile_SteamId\", \"Coins\", \"CriadoEm\", \"UltimoLogin\", \"Profile_Avatar\", \"Profile_Name\", \"Profile_ProfileUrl\", \"Inventario\", \"VipNivel\", \"IsAdmin\")
VALUES ('$USER_ID', '76500000000000099', 500, now(), now(), '', 'TesteUser', 'https://steamcommunity.com/id/teste', '{}', 0, false);
"

node -e "
const crypto = require('crypto');
const key = 'ProjetoZ_Super_Secret_Key_2026_08112005';
function b64url(input) { return Buffer.from(input).toString('base64').replace(/\+/g,'-').replace(/\//g,'_').replace(/=+\$/,''); }
const now = Math.floor(Date.now()/1000);
const header = { alg: 'HS256', typ: 'JWT' };
const payload = { 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '$USER_ID', iss: 'ProjetoZ', aud: 'ProjetoZ', exp: now + 3600, nbf: now, iat: now };
const signingInput = b64url(JSON.stringify(header)) + '.' + b64url(JSON.stringify(payload));
const sig = crypto.createHmac('sha256', key).update(signingInput).digest('base64').replace(/\+/g,'-').replace(/\//g,'_').replace(/=+\$/,'');
require('fs').writeFileSync('token.txt', signingInput + '.' + sig);
"
TOKEN=$(cat token.txt); rm token.txt
```

**Cuidado no Git Bash do Windows**: `node -e "...require('fs').writeFileSync('/tmp/x.txt', ...)"` falha (`ENOENT`, resolve `/tmp` como `Z:\tmp`). Escreva num arquivo relativo ao cwd (ex: `token.txt`) e mova com `mv token.txt /tmp/` se precisar, ou simplesmente leia com `cat token.txt` na mesma pasta.

Pra testar como admin, insira com `"IsAdmin", true`. Pra testar VIP/YouTube vinculado/etc, sete os campos correspondentes direto no `INSERT`.

## 4. Testar via curl

```bash
curl -s -H "Authorization: Bearer $TOKEN" http://localhost/api/auth/me
curl -s -H "Authorization: Bearer $TOKEN" -X POST http://localhost/api/algum/endpoint -H "Content-Type: application/json" -d '{"campo":"valor"}'
```

Pra endpoints do `GameController` (mod do jogo), não usa JWT — usa a chave compartilhada no corpo (`GAMESERVER_API_KEY` do `.env` local, ou o valor de teste `local_test_key_only` se já estiver setado):

```bash
curl -s -X POST http://localhost/api/game/player -H "Content-Type: application/json" -d '{"apiKey":"local_test_key_only","steamId":"76500000000000099"}'
```

## 5. Testar no navegador (Browser pane)

```
preview_start({ url: "http://localhost/Home" })
```

Depois injeta o token:
```js
localStorage.setItem('token', '<TOKEN>'); location.href='/Alguma/Pagina';
```

Pra testar `confirm()` nativo sem travar (ex: botão de excluir), estuba **depois** de já estar na página certa (o stub não sobrevive a um `location.href`):
```js
window.confirm = () => true;
```

## 6. Limpar os dados de teste

Sempre no final, mesmo se algo deu errado no meio:
```bash
docker exec projetoz-postgres psql -U postgres -d projetoz -c "DELETE FROM \"Users\" WHERE \"Id\" = '$USER_ID';"
```

Cuidado com FKs manuais — se o usuário de teste gerou linhas em `Compras`, `Clipes`, `ClipeCurtidas`, `SorteioParticipantes` etc., apague essas primeiro.
