using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Application.Identity.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);
    Task UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}
