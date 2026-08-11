using Microsoft.AspNetCore.Mvc;
using ProjetoZ.Api.Services;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/vip")]
public class VipController : ControllerBase
{
    [HttpGet("niveis")]
    public IActionResult GetNiveis()
    {
        var niveis = VipTiers.Nomes
            .OrderBy(kv => kv.Key)
            .Select(kv => new { nivel = kv.Key, nome = kv.Value, duracaoDias = VipTiers.DuracaoDias });

        return Ok(niveis);
    }
}
