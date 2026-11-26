using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Responses;

namespace MiPos.Application.Typologies.Services;

public class TypologyService(IMiPosDbContext dbContext) : ITypologyService
{
    public async Task<List<TypologyResponse>> GetTypologiesAsync(int? groupId, CancellationToken cancellationToken)
    {
        var query = dbContext.Typologies.AsNoTracking();
        if (groupId is int requestedGroupId)
        {
            query = query.Where(typology => typology.GroupId == requestedGroupId);
        }

        return await query
            .OrderBy(typology => typology.GroupId)
            .ThenBy(typology => typology.SortOrder)
            .ThenBy(typology => typology.Code)
            .Select(typology => new TypologyResponse
            {
                Id = typology.Id,
                GroupId = typology.GroupId,
                Code = typology.Code,
                Name = typology.Name,
                Description = typology.Description,
                SortOrder = typology.SortOrder,
                IsActive = typology.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TypologyResponse> GetTypologyByIdAsync(int typologyId, CancellationToken cancellationToken)
    {
        var typology = await dbContext.Typologies.AsNoTracking()
            .SingleOrDefaultAsync(typology => typology.Id == typologyId, cancellationToken)
            ?? throw new NotFoundException("Tipología no encontrada", "No existe una tipología con ese identificador.");

        return new TypologyResponse
        {
            Id = typology.Id,
            GroupId = typology.GroupId,
            Code = typology.Code,
            Name = typology.Name,
            Description = typology.Description,
            SortOrder = typology.SortOrder,
            IsActive = typology.IsActive
        };
    }
}
