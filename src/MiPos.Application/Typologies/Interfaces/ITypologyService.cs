using MiPos.Application.Typologies.Responses;

namespace MiPos.Application.Typologies.Interfaces;

public interface ITypologyService
{
    Task<List<TypologyResponse>> GetTypologiesAsync(int? groupId, CancellationToken cancellationToken);

    Task<TypologyResponse> GetTypologyByIdAsync(int typologyId, CancellationToken cancellationToken);
}
