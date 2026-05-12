using Microsoft.AspNetCore.Mvc;
using Pm.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace Pm.Controllers;

[Route("api/[controller]")]
[ApiController]
[SwaggerTag("Verificação de saúde da API")]
public class HealthController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpGet("")]
        [SwaggerOperation(
        Summary = "Verifica saúde da API"
    )]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> Healthcheck()
    {
        return Ok("🫡 Ready to work!");
    }

}