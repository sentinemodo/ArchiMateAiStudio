using Microsoft.AspNetCore.Mvc;

namespace ArchiMateAiStudio.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "ok", product = "ArchiMate AI Studio", apiVersion = "v1" });
}
