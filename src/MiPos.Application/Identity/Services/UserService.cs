using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Application.Identity.Services;

public class UserService(IMiPosDbContext dbContext, IRequestContext requestContext, IPasswordHasher<User> passwordHasher)
    : IUserService
{
    public Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return (
            from user in QueryBusinessUsers().AsNoTracking()
            join role in dbContext.Roles on new { user.RoleId, user.BusinessId }
                equals new { RoleId = role.Id, role.BusinessId }
            where role.Code != RoleCodes.Administrator
            orderby user.NormalizedUsername
            select new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleCode = role.Code,
                IsActive = user.IsActive
            }).ToListAsync(cancellationToken);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var role = await FindAssignableRoleAsync(request.RoleId!.Value, cancellationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            BusinessId = requestContext.BusinessId,
            RoleId = role.Id,
            Username = request.Username.Trim(),
            NormalizedUsername = User.NormalizeUsername(request.Username),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = string.Empty
        };
        await EnsureUsernameAvailableAsync(user.Id, user.NormalizedUsername, cancellationToken);
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleCode = role.Code,
            IsActive = user.IsActive
        };
    }

    public async Task UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await FindBusinessUserAsync(userId, cancellationToken);
        var role = await FindAssignableRoleAsync(request.RoleId!.Value, cancellationToken);
        if (user.RoleId != role.Id)
        {
            await EnsureAnotherActiveAdministratorAsync(user, cancellationToken);
        }

        await ApplyProfileAsync(user, request, cancellationToken);
        user.RoleId = role.Id;
        await SaveUserAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await FindBusinessUserAsync(userId, cancellationToken);
        if (user.IsActive == isActive)
        {
            return;
        }

        if (isActive)
        {
            await FindAssignableRoleAsync(user.RoleId, cancellationToken);
        }
        else
        {
            await EnsureAnotherActiveAdministratorAsync(user, cancellationToken);
            if (user.Id == requestContext.UserId)
            {
                throw new BusinessValidationException("Estado de usuario inválido", "No puedes desactivar tu propio usuario.");
            }
        }

        user.IsActive = isActive;
        await SaveUserAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

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
        await ApplyProfileAsync(user, request, cancellationToken);
        await SaveUserAsync(user, cancellationToken);
    }

    private async Task ApplyProfileAsync(User user, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var normalizedUsername = User.NormalizeUsername(request.Username);
        await EnsureUsernameAvailableAsync(user.Id, normalizedUsername, cancellationToken);

        user.Username = request.Username.Trim();
        user.NormalizedUsername = normalizedUsername;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
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

    private async Task EnsureUsernameAvailableAsync(Guid userId, string normalizedUsername, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(candidate => candidate.Id != userId &&
            candidate.NormalizedUsername == normalizedUsername, cancellationToken))
        {
            throw new ConflictException("Usuario duplicado", "Ese nombre de usuario ya está en uso.");
        }
    }

    private IQueryable<User> QueryBusinessUsers()
    {
        var businessId = requestContext.BusinessId;
        return dbContext.Users.Where(user => user.BusinessId == businessId && user.DeletedAt == null);
    }

    private IQueryable<User> QueryCurrentUser()
    {
        var userId = requestContext.UserId;
        return QueryBusinessUsers().Where(user => user.Id == userId && user.IsActive);
    }

    private async Task<User> FindBusinessUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await QueryBusinessUsers().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado", "No se encontró el usuario en el negocio.");

    private async Task<Role> FindAssignableRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var businessId = requestContext.BusinessId;
        return await dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(role =>
            role.Id == roleId && role.BusinessId == businessId && role.IsActive && role.DeletedAt == null, cancellationToken)
            ?? throw new BusinessValidationException("Rol inválido", "Selecciona un rol activo del negocio.");
    }

    private async Task EnsureAnotherActiveAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.IsActive)
        {
            return;
        }

        var isAdministrator = await dbContext.Roles.AnyAsync(role => role.Id == user.RoleId &&
            role.BusinessId == user.BusinessId && role.Code == RoleCodes.Administrator &&
            role.IsActive && role.DeletedAt == null, cancellationToken);

        if (isAdministrator && !await QueryBusinessUsers().AnyAsync(candidate =>
            candidate.Id != user.Id && candidate.RoleId == user.RoleId && candidate.IsActive, cancellationToken))
        {
            throw new ConflictException("Último administrador activo", "El negocio debe conservar al menos un administrador activo.");
        }
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
