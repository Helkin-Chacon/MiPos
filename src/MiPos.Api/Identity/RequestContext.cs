using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using MiPos.Application.Common.Interfaces;

namespace MiPos.Api.Identity;

public class RequestContext(IHttpContextAccessor httpContext) : IRequestContext
{
    private ClaimsPrincipal User => httpContext.HttpContext?.User is { Identity.IsAuthenticated: true } user
        ? user
        : throw new InvalidOperationException("No authenticated user is available.");

    public Guid UserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    public Guid BusinessId => Guid.Parse(User.FindFirstValue(JwtTokenGenerator.BusinessIdClaim)!);
    public string RoleCode => User.FindFirstValue(JwtTokenGenerator.RoleClaim) ?? "";
    public IReadOnlyList<string> Permissions => User.FindAll(JwtTokenGenerator.PermissionClaim).Select(claim => claim.Value).ToArray();
}
