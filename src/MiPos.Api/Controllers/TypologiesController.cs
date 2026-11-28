using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/typologies")]
public class TypologiesController(
    ITypologyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TypologyResponse>>>> GetTypologies([FromQuery] int? groupId, CancellationToken cancellationToken)
    {
        var typologies = await service.GetTypologiesAsync(groupId, cancellationToken);

        return Ok(new ApiResponse<List<TypologyResponse>> { Data = typologies });
    }

    [HttpGet("{typologyId:int}")]
    public async Task<ActionResult<ApiResponse<TypologyResponse>>> GetTypologyById(int typologyId, CancellationToken cancellationToken)
    {
        var typology = await service.GetTypologyByIdAsync(typologyId, cancellationToken);

        return Ok(new ApiResponse<TypologyResponse> { Data = typology });
    }
}
