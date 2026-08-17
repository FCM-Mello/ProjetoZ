using Microsoft.AspNetCore.Authorization;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Authorization;

// Registrado como requirement da DefaultPolicy (Program.cs) — roda em TODO
// endpoint [Authorize] do site (não só os que usam a policy "Admin"), pra
// que um usuário banido perca acesso na hora, mesmo com um JWT ainda válido
// de uma sessão aberta antes do banimento.
public class NotBannedAuthorizationHandler : AuthorizationHandler<NotBannedRequirement>
{
    private readonly ApplicationDbContext _context;

    public NotBannedAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, NotBannedRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null || !Guid.TryParse(userId, out var id))
            return;

        var user = await _context.Users.FindAsync(id);

        if (user != null && !user.Banido)
        {
            context.Succeed(requirement);
        }
    }
}
