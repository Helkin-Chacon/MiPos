using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Organization.Interfaces;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/branches")]
public class BranchesController(IBranchService service, IRequestContext requestContext) : ControllerBase
{
    [HttpGet("/api/v1/businesses/{businessId:guid}/branches")]
    public async Task<ActionResult<ApiResponse<List<BranchResponse>>>> GetBranchesByBusinessId(Guid businessId, CancellationToken cancellationToken)
    {
        if (businessId != requestContext.BusinessId)
        {
            return NotFound();
        }

        var branches = await service.GetBranchesByBusinessIdAsync(businessId, cancellationToken);
        return Ok(new ApiResponse<List<BranchResponse>> { Data = branches });
    }

    [HttpGet("{branchId:guid}")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> GetBranchById(Guid branchId, CancellationToken cancellationToken)
    {
        var branch = await service.GetBranchByIdAsync(requestContext.BusinessId, branchId, cancellationToken);
        return Ok(new ApiResponse<BranchResponse> { Data = branch });
    }

    [HttpPost("/api/v1/businesses/{businessId:guid}/branches")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> CreateBranch(
        Guid businessId, CreateBranchRequest request, CancellationToken cancellationToken)
    {
        if (businessId != requestContext.BusinessId)
        {
            return NotFound();
        }

        var branch = await service.CreateBranchAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetBranchById), new { branchId = branch.Id },
            new ApiResponse<BranchResponse> { Data = branch });
    }
}
