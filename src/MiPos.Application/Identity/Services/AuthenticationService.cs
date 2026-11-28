using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Models;
using MiPos.Application.Identity.Requests;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Application.Identity.Services;

public class AuthenticationService(IMiPosDbContext dbContext, IPasswordHasher<User> hasher) : IAuthenticationService
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(string businessCode, LoginRequest request,
        CancellationToken cancellationToken)
    {
        var businessId = await dbContext.Businesses
            .Where(business => business.Code == businessCode && business.IsActive && business.DeletedAt == null)
            .Select(business => (Guid?)business.Id).SingleOrDefaultAsync(cancellationToken);
        if (businessId is null)
        {
            return null;
        }

        var username = User.NormalizeUsername(request.Username);
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user =>
            user.BusinessId == businessId && user.NormalizedUsername == username && user.IsActive && user.DeletedAt == null,
            cancellationToken);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var roleCode = await dbContext.Roles
            .Where(role => role.Id == user.RoleId && role.BusinessId == user.BusinessId && role.IsActive && role.DeletedAt == null)
            .Select(role => role.Code).SingleOrDefaultAsync(cancellationToken);
        if (roleCode is null)
        {
            return null;
        }

        var permissions = await (
            from assignment in dbContext.RolePermissions
            join permission in dbContext.Permissions on assignment.PermissionId equals permission.Id
            where assignment.BusinessId == user.BusinessId && assignment.RoleId == user.RoleId
            orderby permission.Code
            select permission.Code).ToListAsync(cancellationToken);

        return new AuthenticatedUser
        {
            UserId = user.Id, BusinessId = user.BusinessId, RoleCode = roleCode, Permissions = permissions
        };
    }
}
