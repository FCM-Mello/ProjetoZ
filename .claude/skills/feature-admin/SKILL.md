---
name: feature-admin
description: Painel de administração — gerenciar coins/VIP/inventário de qualquer usuário, promover/remover admin, com proteção contra auto-remoção e um SteamID que nunca pode perder o acesso. Use ao mexer em qualquer coisa relacionada ao painel de admin ou ao controle de quem é admin.
---

# Painel de administração

Status de admin é um campo no banco (`User.IsAdmin`, bool), **não** mais um arquivo `admins.json`. Isso mudou nesta base: originalmente era um JSON montado como volume read-only no container — impossível de editar em runtime — então foi migrado pra uma coluna, com uma migration (`AddUserIsAdmin`) que já preserva o admin original (SteamID `76561198886359962`) na transição.

## Verificação de admin

`backend/ProjetoZ.Api/Authorization/AdminAuthorizationHandler.cs` — busca o `User` pelo claim do JWT e checa `user.IsAdmin` direto no banco (sem cache, sem singleton). Usado via `[Authorize(Policy = "Admin")]`, política registrada em `Program.cs`.

## Endpoints (`backend/ProjetoZ.Api/Controllers/AdminController.cs`, tudo `[Authorize(Policy = "Admin")]`)

- `GET /api/admin/usuarios?busca=` — lista/busca por nome ou SteamID.
- `GET /api/admin/usuarios/{id}` — detalhe com inventário.
- `POST /api/admin/usuarios/{id}/coins` (`{delta: int}`) — soma/subtrai, clampado em 0 (`Math.Max(0, ...)`).
- `POST /api/admin/usuarios/{id}/coins/zerar`.
- `POST /api/admin/usuarios/{id}/vip` (`{nivel: int}`) — concede, sempre 30 dias fixos a partir de agora.
- `DELETE /api/admin/usuarios/{id}/vip`.
- `POST /api/admin/usuarios/{id}/inventario` (`{produtoId}`) / `DELETE .../inventario/{produtoId}` — adiciona/remove **uma** ocorrência (produto pode repetir na lista).
- `POST /api/admin/usuarios/{id}/admin` — torna admin.
- `DELETE /api/admin/usuarios/{id}/admin` — remove admin, **com duas proteções**:
  1. Ninguém remove o próprio acesso (`meuId == id` → 400), mesmo sendo admin.
  2. O SteamID `76561198886359962` (dono do site) nunca perde o admin, **mesmo se outro admin tentar** — checagem hardcoded (`SuperAdminSteamId` no controller) antes da checagem de auto-remoção. Se precisar trocar o dono, é esse valor que muda.

## Frontend

`frontend/app/Admin/page.tsx` (lista/busca) + `components/UsuarioAdminModal.tsx` (painel de edição por usuário, com as mesmas duas proteções replicadas no client — botão desabilitado, mas a garantia real é sempre a checagem do backend). Rota protegida por `frontend/app/hooks/useRequireAdmin.ts` (client-side só, redireciona se `!user.isAdmin` — não é uma proteção de segurança real, é UX; a segurança vem da API).
