using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Responses;

namespace MiPos.Application.Typologies.Services;

public sealed class TypologyService(IMiPosDbContext dbContext) : ITypologyService
{
    public async Task<List<TypologyResponse>> GetAllAsync(int? groupId, CancellationToken cancellationToken)
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
            .Select(typology => new TypologyResponse(typology.Id, typology.GroupId, typology.Code,
                typology.Name, typology.Description, typology.SortOrder, typology.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<TypologyResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var typology = await dbContext.Typologies.AsNoTracking()
            .SingleOrDefaultAsync(typology => typology.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tipología no encontrada", "No existe una tipología con ese identificador.");

        return new TypologyResponse(typology.Id, typology.GroupId, typology.Code,
            typology.Name, typology.Description, typology.SortOrder, typology.IsActive);
    }
}
