using MiPos.Application.Typologies.Responses;

namespace MiPos.Application.Typologies.Interfaces;

public interface ITypologyService
{
    Task<List<TypologyResponse>> GetAllAsync(int? groupId, CancellationToken cancellationToken);

    Task<TypologyResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
