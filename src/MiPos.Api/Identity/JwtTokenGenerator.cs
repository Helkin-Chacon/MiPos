using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MiPos.Application.Identity.Models;

namespace MiPos.Api.Identity;

public class JwtTokenGenerator(IOptions<JwtOptions> configuration)
{
    internal const string BusinessIdClaim = "bid";
    internal const string RoleClaim = "role";
    internal const string PermissionClaim = "permission";

    public string Generate(AuthenticatedUser user)
    {
        var options = configuration.Value;
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(options.AccessTokenHours),
            SigningCredentials = new SigningCredentials(GetSigningKey(options), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.UserId.ToString(),
                [BusinessIdClaim] = user.BusinessId.ToString(),
                [RoleClaim] = user.RoleCode,
                [PermissionClaim] = user.Permissions.ToArray()
            }
        });
    }

    internal static SymmetricSecurityKey GetSigningKey(JwtOptions options) => new(Convert.FromBase64String(options.SigningKey));
}
