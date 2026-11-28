using Microsoft.EntityFrameworkCore;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;
using MiPos.Domain.Entities.Typologies;

namespace MiPos.Application.Common.Interfaces;

public interface IMiPosDbContext
{
    DbSet<Typology> Typologies { get; }
    DbSet<Business> Businesses { get; }
    DbSet<Branch> Branches { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
