using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/roles")]
public class RolesController(IRoleService roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await roles.GetRolesAsync(cancellationToken);
        return Ok(new ApiResponse<List<RoleResponse>> { Data = result });
    }

    [HttpPut("{roleId:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRolePermissions(
        Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        await roles.UpdateRolePermissionsAsync(roleId, request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }
}
