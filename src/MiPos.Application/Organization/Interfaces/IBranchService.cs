using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;

namespace MiPos.Application.Organization.Interfaces;

public interface IBranchService
{
    Task<List<BranchResponse>> GetBranchesByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken);

    Task<BranchResponse> GetBranchByIdAsync(Guid businessId, Guid branchId, CancellationToken cancellationToken);

    Task<BranchResponse> CreateBranchAsync(Guid businessId, CreateBranchRequest request, CancellationToken cancellationToken);
}
