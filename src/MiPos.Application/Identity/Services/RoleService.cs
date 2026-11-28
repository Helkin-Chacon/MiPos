using System.Data;
using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Application.Identity.Services;

public class RoleService(IMiPosDbContext dbContext, IRequestContext requestContext) : IRoleService
{
    public Task<List<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        QueryPermissions().OrderBy(permission => permission.Code).ToListAsync(cancellationToken);

    public Task<List<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return QueryBusinessRoles().AsNoTracking().OrderBy(role => role.Code)
            .Select(role => new RoleResponse
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                IsActive = role.IsActive,
                Permissions = (
                    from assignment in dbContext.RolePermissions
                    join permission in QueryPermissions() on assignment.PermissionId equals permission.Id
                    where assignment.RoleId == role.Id && assignment.BusinessId == role.BusinessId
                    orderby permission.Code
                    select permission).ToList()
            }).ToListAsync(cancellationToken);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var role = await QueryBusinessRoles().SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken)
            ?? throw new NotFoundException("Rol no encontrado", "No se encontró el rol en el negocio.");

        if (role.Code == RoleCodes.Administrator)
        {
            throw new BusinessValidationException("Rol protegido", "Los permisos del rol administrador no se pueden modificar.");
        }

        var permissionIds = request.PermissionIds.ToHashSet();
        if (permissionIds.Count != request.PermissionIds.Count)
        {
            throw new BusinessValidationException("Permisos duplicados", "Envía cada permiso una sola vez.");
        }

        var existingPermissionCount = await dbContext.Permissions.CountAsync(
            permission => permissionIds.Contains(permission.Id), cancellationToken);
        if (existingPermissionCount != permissionIds.Count)
        {
            throw new BusinessValidationException("Permisos inválidos", "Uno o más permisos no existen en el catálogo.");
        }

        // Check the role version before changing its assignments within the same transaction.
        role.UpdatedAt = DateTime.UtcNow;
        role.Version++;
        await dbContext.SaveChangesAsync(cancellationToken);

        var assignments = await dbContext.RolePermissions
            .Where(assignment => assignment.RoleId == role.Id && assignment.BusinessId == role.BusinessId)
            .ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(assignments.Where(assignment => !permissionIds.Contains(assignment.PermissionId)));
        dbContext.RolePermissions.AddRange(permissionIds.Except(assignments.Select(assignment => assignment.PermissionId))
            .Select(permissionId => new RolePermission
            {
                BusinessId = role.BusinessId,
                RoleId = role.Id,
                PermissionId = permissionId
            }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<Role> QueryBusinessRoles()
    {
        var businessId = requestContext.BusinessId;
        return dbContext.Roles.Where(role => role.BusinessId == businessId && role.DeletedAt == null);
    }

    private IQueryable<PermissionResponse> QueryPermissions() =>
        dbContext.Permissions.AsNoTracking().Select(permission => new PermissionResponse
        {
            Id = permission.Id,
            Code = permission.Code,
            Name = permission.Name,
            Description = permission.Description
        });
}
