using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Application.Identity.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}
