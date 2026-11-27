using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Interfaces;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Application.Identity.Services;

internal static class DefaultBusinessRoles
{
    internal static async Task AddAsync(IMiPosDbContext dbContext, Business business, CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        var sell = permissions.Single(permission => permission.Code == PermissionCodes.PosSell);
        var openClose = permissions.Single(permission => permission.Code == PermissionCodes.CashRegisterOpenClose);

        AddRole(dbContext, business, RoleCodes.Administrator, "Administrador", permissions);
        AddRole(dbContext, business, RoleCodes.Cashier, "Cajero", [sell, openClose]);
    }

    private static void AddRole(
        IMiPosDbContext dbContext, Business business, string code, string name, IEnumerable<Permission> permissions)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Code = code,
            Name = name,
            CreatedAt = business.CreatedAt,
            UpdatedAt = business.UpdatedAt
        };

        dbContext.Roles.Add(role);
        dbContext.RolePermissions.AddRange(permissions.Select(permission => new RolePermission
        {
            BusinessId = business.Id,
            RoleId = role.Id,
            PermissionId = permission.Id
        }));
    }
}
