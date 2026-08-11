using Microsoft.AspNetCore.Authorization;
using ProjetoZ.Api.Services;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Authorization;

public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly AdminsProvider _adminsProvider;

    public AdminAuthorizationHandler(ApplicationDbContext context, AdminsProvider adminsProvider)
    {
        _context = context;
        _adminsProvider = adminsProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null || !Guid.TryParse(userId, out var id))
            return;

        var user = await _context.Users.FindAsync(id);

        if (_adminsProvider.IsAdmin(user?.Profile?.SteamId))
        {
            context.Succeed(requirement);
        }
    }
}
