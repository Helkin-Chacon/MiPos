using Microsoft.AspNetCore.Mvc;

namespace MiPos.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/plain")]
    public ContentResult Get() => Content("Healthy", "text/plain");
}
