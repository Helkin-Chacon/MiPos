using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Services;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Api.Identity;

internal static class AuthenticationConfiguration
{
    internal static void AddMiPosAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<JwtTokenGenerator>();
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestContext, RequestContext>();

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"))
            .Validate(options => Convert.TryFromBase64String(options.SigningKey, new byte[32], out var length) && length == 32,
                "Configure Jwt:SigningKey with 32 random bytes in Base64.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience),
                "JWT issuer and audience are required.")
            .Validate(options => options.AccessTokenHours > 0, "JWT duration must be positive.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, configured) =>
            {
                var options = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.IncludeErrorDetails = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = JwtTokenGenerator.GetSigningKey(options),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }
}
