using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;

namespace MiPos.Application.Organization.Interfaces;

public interface IBusinessService
{
    Task<BusinessResponse> GetBusinessByIdAsync(Guid businessId, CancellationToken cancellationToken);

    Task<BusinessResponse> CreateBusinessAsync(CreateBusinessRequest request, CancellationToken cancellationToken);
}
