---
name: feature-clipes
description: Ranking semanal de clipes do YouTube com verificação de dono via OAuth do Google, fechamento automático e premiação em Az Coins. Use ao mexer em qualquer coisa relacionada a clipes, vínculo de YouTube, ou no fechamento semanal.
---

# Ranking semanal de clipes

Usuários vinculam o canal do YouTube (OAuth Google), postam vídeo do próprio canal publicado durante a semana atual, outros curtem, e no fim da semana o clipe mais curtido dá 500 Az Coins pro autor. Os clipes são apagados no fechamento.

## Vínculo com o YouTube (OAuth do Google)

`backend/ProjetoZ.Api/Controllers/AuthController.cs`:
- `GET /api/auth/youtube/vincular?token=<jwt>` — **não usa `[Authorize]`** porque é uma navegação de página inteira (`window.location.href`, não um `fetch`), então não dá pra mandar o header `Authorization`. O JWT do ArkZ vem por query string e é validado manualmente (`JwtService.ValidarEExtrairUserId`), guardado em `AuthenticationProperties.Items["arkzUserId"]` pra sobreviver o vai-e-volta do OAuth do Google, depois disparado `Challenge(properties, "Google")`.
- `GET /api/auth/youtube/callback` — recebe o retorno do Google, usa o access token pra chamar `YoutubeService.ObterCanalAsync` e salva `User.YoutubeChannelId`/`YoutubeChannelNome`.
- `DELETE /api/auth/youtube/vincular` — `[Authorize]` normal (é um fetch, não navegação), limpa os dois campos.

Registro do esquema Google em `Program.cs` (`.AddGoogle("Google", ...)`) com `CallbackPath = "/signin-google"` e escopo `youtube.readonly`.

**Gotcha de infra**: `/signin-google` (e `/signin-steam`) ficam fora do prefixo `/api/`, então o nginx precisa de um `location` específico apontando pra API (`nginx/default.conf` e `default-ssl.conf`) — sem isso cai no bloco geral que manda tudo pro frontend e dá 404 (já aconteceu, ver commit `fb3a953`).

**Gotcha do Google Console**: o app fica em modo "Teste" até alguém clicar "Publicar aplicativo" na Tela de permissão OAuth — nesse modo só e-mails cadastrados como testador conseguem logar. `youtube.readonly` é escopo "Sensível" (não "Restrito"), então publicar já libera pra todo mundo sem precisar de revisão formal do Google (só aparece um aviso de "app não verificado" que dá pra pular). Publicar exige política de privacidade preenchida (`frontend/app/Privacidade`) e o nome/logo do app batendo com o que aparece na home do site.

## Postar um clipe — todas as regras (backend/ProjetoZ.Api/Controllers/ClipesController.cs, `Create`)

Validado nessa ordem, todas via `YoutubeService.ObterInfoDoVideoAsync` (usa `Google:YoutubeApiKey`, não o token do usuário):
1. Usuário precisa ter `YoutubeChannelId` preenchido (vinculado).
2. `channelId` real do vídeo bate com `usuario.YoutubeChannelId`.
3. `publishedAt` real do vídeo é `>= Semana.InicioSemanaAtualUtc()` (não pode ser vídeo antigo reaproveitado).
4. Título real do vídeo no YouTube contém "ArkZ" (case-insensitive).

## Curtir e excluir

- `POST /api/clipes/{id}/curtir` — idempotente, bloqueia curtir o próprio clipe.
- `DELETE /api/clipes/{id}` — autor do clipe ou admin (`user.IsAdmin`), qualquer outro dá 403.

## Fechamento semanal (`backend/ProjetoZ.Api/Services/FechamentoSemanalClipesService.cs`)

`BackgroundService`, checa a cada 10 min se a semana virou (`Semana.InicioSemanaAtualUtc()`, sempre segunda 00h em `America/Sao_Paulo`). Quando vira:
- Acha, por usuário, o clipe com mais curtidas; entre os usuários, o maior valor vence; empate resolvido com `Random.Shared.Next`.
- Credita 500 Az Coins, registra `Compra` (`Tipo = "clipe"`).
- Salva um **snapshot** do vencedor em `ClipeConfig` (`UltimoVencedorTitulo`/`Url`/`AutorNome`/`AutorAvatar`/`Curtidas`/`FechadoEm`) — necessário porque os clipes são apagados logo em seguida, então sem isso não sobraria nada pra mostrar "vencedor da semana passada" na tela.
- Apaga todos os `Clipe`/`ClipeCurtida`.
- `ClipeConfig` é uma linha singleton (`Id = 1`) que também guarda `UltimoFechamento`, usado pra não fechar a mesma semana duas vezes mesmo com restart do container.

## Frontend

`frontend/app/Clipes/page.tsx` — cartão de destaque "Vencedor da semana passada" (lê `ultimoVencedor` da resposta de `GET /api/clipes`), grade de clipes com embed do YouTube (`<iframe>` extraindo o ID do vídeo via regex), botão de curtir, botão de excluir (autor/admin), botão vincular/desvincular YouTube. `components/ClipeModal.tsx` é o form de postar.
