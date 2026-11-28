using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiPos.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/plain")]
    public ContentResult GetHealth() => Content("Healthy", "text/plain");
}
