using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Application.Identity.Interfaces;

public interface IRoleService
{
    Task<List<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<List<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);
    Task UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken);
}
