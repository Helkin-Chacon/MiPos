using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Application.Identity.Services;

public class UserService(IMiPosDbContext dbContext, IRequestContext requestContext, IPasswordHasher<User> passwordHasher)
    : IUserService
{
    public async Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await (
            from user in QueryCurrentUser().AsNoTracking()
            join role in dbContext.Roles on new { user.RoleId, user.BusinessId } equals new { RoleId = role.Id, role.BusinessId }
            select new UserProfileResponse
            {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                BusinessId = user.BusinessId,
                RoleCode = role.Code
            }).SingleOrDefaultAsync(cancellationToken) ?? throw UserNotFound();
    }

    public async Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(cancellationToken);
        var normalizedUsername = User.NormalizeUsername(request.Username);
        if (await dbContext.Users.AnyAsync(candidate => candidate.Id != user.Id &&
            candidate.NormalizedUsername == normalizedUsername, cancellationToken))
        {
            throw new ConflictException("Usuario duplicado", "Ese nombre de usuario ya está en uso.");
        }

        user.Username = request.Username.Trim();
        user.NormalizedUsername = normalizedUsername;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        await SaveUserAsync(user, cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(cancellationToken);
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw new BusinessValidationException("Contraseña incorrecta", "La contraseña actual no es válida.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await SaveUserAsync(user, cancellationToken);
    }

    private IQueryable<User> QueryCurrentUser()
    {
        var userId = requestContext.UserId;
        var businessId = requestContext.BusinessId;
        return dbContext.Users.Where(user => user.Id == userId && user.BusinessId == businessId &&
            user.IsActive && user.DeletedAt == null);
    }

    private async Task<User> FindCurrentUserAsync(CancellationToken cancellationToken) =>
        await QueryCurrentUser().SingleOrDefaultAsync(cancellationToken) ?? throw UserNotFound();

    private Task SaveUserAsync(User user, CancellationToken cancellationToken)
    {
        user.UpdatedAt = DateTime.UtcNow;
        user.Version++;
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NotFoundException UserNotFound() =>
        new("Usuario no encontrado", "No se encontró el usuario autenticado.");
}
