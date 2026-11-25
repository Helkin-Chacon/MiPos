using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Route("api/v1/typologies")]
public sealed class TypologiesController(
    ITypologyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TypologyResponse>>>> GetAll([FromQuery] int? groupId, CancellationToken cancellationToken)
    {
        var typologies = await service.GetAllAsync(groupId, cancellationToken);

        return Ok(new ApiResponse<List<TypologyResponse>>(typologies, null));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TypologyResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var typology = await service.GetByIdAsync(id, cancellationToken);

        return Ok(new ApiResponse<TypologyResponse>(typology, null));
    }
}
