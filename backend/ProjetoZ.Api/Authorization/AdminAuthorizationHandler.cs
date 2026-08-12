using Microsoft.AspNetCore.Authorization;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Authorization;

public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly ApplicationDbContext _context;

    public AdminAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null || !Guid.TryParse(userId, out var id))
            return;

        var user = await _context.Users.FindAsync(id);

        if (user != null && user.IsAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
