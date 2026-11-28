using MiPos.Application.Identity.Models;
using MiPos.Application.Identity.Requests;

namespace MiPos.Application.Identity.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticatedUser?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken);
}
