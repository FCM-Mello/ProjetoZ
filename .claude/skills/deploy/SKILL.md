---
name: deploy
description: Deploy do ArkZ pro servidor de produção (arkz.dev.br via SSH), incluindo o passo de migration quando há mudança de schema. Use sempre que o usuário pedir pra subir mudanças pra produção.
---

# Deploy em produção

Servidor: Vultr VPS em `216.238.107.240`, acesso via SSH como root, projeto em `~/ProjetoZ`. `arkz.dev.br` aponta pra esse IP.

## Regra de ouro: migration ANTES do código

Se o commit inclui uma migration nova do EF Core, ela **precisa** rodar contra o banco de produção **antes** de subir o container da API com o código novo — o código novo já espera as colunas/tabelas novas existirem. Rodar na ordem errada derruba o site inteiro (toda query que toca a tabela afetada falha).

## 1. Gerar o bundle da migration (só se houver migration nova)

No terminal local, dentro de `backend/ProjetoZ.Api`:

```bash
cd backend/ProjetoZ.Api
dotnet ef migrations bundle --self-contained -r linux-x64 --project ../ProjetoZ.Persistence --startup-project . -o efbundle-linux --force
```

Gera `backend/ProjetoZ.Api/efbundle-linux` (binário standalone, não precisa do SDK instalado no servidor). Esse arquivo é git-ignorado — nunca é commitado, sempre gerado localmente e copiado na hora.

## 2. Copiar o bundle pro servidor

**Numa janela de terminal local separada — nunca de dentro da sessão SSH já aberta** (rodar `scp` dentro da sessão SSH tenta resolver o caminho `Z:\...` do Windows como se fosse um caminho do servidor Linux e falha com "Could not resolve hostname").

```bash
scp Z:\ProjetoZ\backend\ProjetoZ.Api\efbundle-linux root@216.238.107.240:~/ProjetoZ/backend/
```

## 3. No servidor (via SSH)

```bash
git pull

# só se tiver migration nova:
chmod +x backend/efbundle-linux
./backend/efbundle-linux --connection "Host=localhost;Port=5432;Database=projetoz;Username=postgres;Password=postgres"

docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
docker compose restart nginx
```

O `docker compose restart nginx` no final é **obrigatório**, mesmo quando parece redundante: o nginx não resolve DNS interno do Docker a cada request, só na inicialização — se os containers `api`/`frontend` forem recriados (IP interno novo) e o nginx não for reiniciado, ele continua tentando falar com o IP antigo e o site cai com 502.

Se o servidor já tiver `COMPOSE_FILE=docker-compose.yml:docker-compose.prod.yml` no `~/.bashrc`, dá pra usar só `docker compose up -d --build` sem repetir os `-f`. Ainda assim sempre reinicie o nginx depois.

## Coisas que já causaram incidente aqui

- **Rodar `docker compose up -d --build` sem o `-f docker-compose.prod.yml`** reverte o nginx pro modo HTTP-only silenciosamente (perde HTTPS) porque o compose usa o `docker-compose.yml` base sozinho.
- **Rotas fora de `/api/`** (como `/signin-steam`, `/signin-google` — os callbacks OAuth) precisam de um `location` próprio no nginx apontando pra API; se esquecer, cai no bloco geral que manda tudo pro frontend e dá 404. Isso já aconteceu com o Google OAuth (ver commit `fb3a953`).
- Variáveis de ambiente novas (ex: `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_YOUTUBE_API_KEY`) precisam ser adicionadas manualmente no `.env` do servidor — elas não vêm do `git pull`, o `.env` é git-ignorado por design (contém segredo real).
