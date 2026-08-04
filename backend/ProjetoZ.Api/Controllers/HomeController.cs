using Microsoft.AspNetCore.Mvc;

namespace ProjetoZ.Api.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("API funcionando!");
    }
}