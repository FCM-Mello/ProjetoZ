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
        private readonly YoutubeService _youtubeService;
        private readonly IConfiguration _configuration;

        public AuthController(
            ApplicationDbContext context,
            SteamService steamService,
            JwtService jwtService,
            YoutubeService youtubeService,
            IConfiguration configuration)
        {
            _context = context;
            _steamService = steamService;
            _jwtService = jwtService;
            _youtubeService = youtubeService;
            _configuration = configuration;
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

            var frontendUrl = _configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl não configurado.");

            // Bloqueia aqui pra nunca emitir um token novo pra conta banida —
            // uma sessão já aberta antes do banimento é cortada à parte pelo
            // NotBannedAuthorizationHandler no próximo request autenticado.
            if (user.Banido)
                return Redirect($"{frontendUrl}/Auth/Callback?erro=banido");

            var token = _jwtService.Generate(user);

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

            var vipNivel = VipTiers.NivelEfetivo(user.VipNivel, user.VipExpiraEm);

            return Ok(new UserDto
            {
                Id = user.Id,
                Profile = user.Profile ?? new Domian.Models.SteamProfile(),
                Coins = user.Coins,
                Inventario = inventario,
                IsAdmin = user.IsAdmin,
                VipNivel = vipNivel,
                VipNivelNome = vipNivel > 0 ? VipTiers.NomeDoNivel(vipNivel) : null,
                VipExpiraEm = vipNivel > 0 ? user.VipExpiraEm : null,
                YoutubeChannelNome = user.YoutubeChannelNome
            });
        }

        // A vinculação do YouTube é uma navegação de página inteira (não um
        // fetch), então o token do ArkZ chega por query string em vez do
        // header Authorization — ele é validado manualmente aqui e guardado
        // nas AuthenticationProperties pra sobreviver ao vai-e-volta do OAuth
        // do Google.
        [AllowAnonymous]
        [HttpGet("youtube/vincular")]
        public IActionResult VincularYoutube([FromQuery] string token)
        {
            var userId = _jwtService.ValidarEExtrairUserId(token);

            if (userId == null)
                return Unauthorized();

            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/youtube/callback"
            };

            properties.Items["arkzUserId"] = userId.Value.ToString();

            return Challenge(properties, "Google");
        }

        [AllowAnonymous]
        [HttpGet("youtube/callback")]
        public async Task<IActionResult> YoutubeCallback()
        {
            var frontendUrl = _configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl não configurado.");

            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Properties == null)
                return Redirect($"{frontendUrl}/Clipes?youtube=erro");

            if (!result.Properties.Items.TryGetValue("arkzUserId", out var arkzUserId) ||
                arkzUserId == null ||
                !Guid.TryParse(arkzUserId, out var userId))
                return Redirect($"{frontendUrl}/Clipes?youtube=erro");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Redirect($"{frontendUrl}/Clipes?youtube=erro");

            var accessToken = result.Properties.GetTokenValue("access_token");

            var canal = accessToken != null
                ? await _youtubeService.ObterCanalAsync(accessToken)
                : null;

            if (canal == null)
                return Redirect($"{frontendUrl}/Clipes?youtube=erro");

            var canalJaVinculado = await _context.Users
                .AnyAsync(u => u.Id != user.Id && u.YoutubeChannelId == canal.Value.Id);

            if (canalJaVinculado)
                return Redirect($"{frontendUrl}/Clipes?youtube=ja-vinculado-outra-conta");

            user.YoutubeChannelId = canal.Value.Id;
            user.YoutubeChannelNome = canal.Value.Nome;

            await _context.SaveChangesAsync();

            return Redirect($"{frontendUrl}/Clipes?youtube=vinculado");
        }

        [Authorize]
        [HttpDelete("youtube/vincular")]
        public async Task<IActionResult> DesvincularYoutube()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userId));

            if (user == null)
                return Unauthorized();

            user.YoutubeChannelId = null;
            user.YoutubeChannelNome = null;

            await _context.SaveChangesAsync();

            return Ok();
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
