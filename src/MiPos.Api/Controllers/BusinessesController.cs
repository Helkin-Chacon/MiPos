using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Organization.Interfaces;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Route("api/v1/businesses")]
public class BusinessesController(IBusinessService service, IRequestContext requestContext) : ControllerBase
{
    [HttpGet("{businessId:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessResponse>>> GetBusinessById(Guid businessId, CancellationToken cancellationToken)
    {
        if (businessId != requestContext.BusinessId)
        {
            return NotFound();
        }

        var business = await service.GetBusinessByIdAsync(businessId, cancellationToken);
        return Ok(new ApiResponse<BusinessResponse> { Data = business });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BusinessResponse>>> CreateBusiness(CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var business = await service.CreateBusinessAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBusinessById), new { businessId = business.Id },
            new ApiResponse<BusinessResponse> { Data = business });
    }
}
