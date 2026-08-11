using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoZ.Api.Services;
using ProjetoZ.Application.DTOs;
using ProjetoZ.Domain.Entities;
using ProjetoZ.Persistence;
using System.Security.Claims;

namespace ProjetoZ.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SteamService _steamService;
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly AdminsProvider _adminsProvider;

        public AuthController(
            ApplicationDbContext context,
            SteamService steamService,
            JwtService jwtService,
            IConfiguration configuration,
            AdminsProvider adminsProvider)
        {
            _context = context;
            _steamService = steamService;
            _jwtService = jwtService;
            _configuration = configuration;
            _adminsProvider = adminsProvider;
        }

        [HttpGet("steam/login")]
        public IActionResult SteamLogin()
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = "/api/auth/steam/callback"
                },
                "Steam");
        }

        [AllowAnonymous]
        [HttpGet("steam/callback")]
        public async Task<IActionResult> SteamCallback()
        {
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return Unauthorized();

            var steamId = result.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(steamId))
                return Unauthorized();

            steamId = steamId.Split('/').Last();

            var profile = await _steamService.GetProfileAsync(steamId);

            if (profile == null)
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Profile != null && x.Profile.SteamId == steamId);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Profile = profile,
                    CriadoEm = DateTime.UtcNow,
                    UltimoLogin = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }
            else
            {
                user.Profile = profile;
                user.UltimoLogin = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var token = _jwtService.Generate(user);

            var frontendUrl = _configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl não configurado.");

            return Redirect(
                $"{frontendUrl}/Auth/Callback?token={Uri.EscapeDataString(token)}");
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));

            if (user == null)
                return Unauthorized();

            var idsUnicos = user.Inventario.Distinct().ToList();

            var produtosPorId = await _context.Products
                .Where(p => idsUnicos.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Preserva a multiplicidade: o mesmo produto pode aparecer várias
            // vezes em Inventario quando o usuário compra mais de uma unidade.
            var inventario = user.Inventario
                .Where(id => produtosPorId.ContainsKey(id))
                .Select(id => produtosPorId[id])
                .ToList();

            return Ok(new UserDto
            {
                Id = user.Id,
                Profile = user.Profile ?? new Domian.Models.SteamProfile(),
                Coins = user.Coins,
                Inventario = inventario,
                IsAdmin = _adminsProvider.IsAdmin(user.Profile?.SteamId)
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpGet("debug")]
        public IActionResult Debug()
        {
            return Ok(new
            {
                Request.Scheme,
                Host = Request.Host.Value,
                Url = $"{Request.Scheme}://{Request.Host}"
            });
        }
    }
}
