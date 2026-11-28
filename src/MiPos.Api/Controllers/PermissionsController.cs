using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/permissions")]
public class PermissionsController(IRoleService roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PermissionResponse>>>> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await roles.GetPermissionsAsync(cancellationToken);
        return Ok(new ApiResponse<List<PermissionResponse>> { Data = result });
    }
}
