using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Identity;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;

namespace MiPos.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthenticationService authentication, IConfiguration configuration, JwtTokenGenerator tokens)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var businessCode = configuration["Installation:BusinessCode"]?.Trim();
        if (string.IsNullOrEmpty(businessCode))
        {
            throw new InvalidOperationException("Configure Installation:BusinessCode for this installation.");
        }

        var user = await authentication.AuthenticateAsync(businessCode, request, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ApiResponse<LoginResponse>
            {
                Error = ApiError.FromRequest(HttpContext, "Credenciales inválidas", "El usuario o la contraseña no son válidos.")
            });
        }

        return Ok(new ApiResponse<LoginResponse> { Data = new LoginResponse { AccessToken = tokens.Generate(user) } });
    }
}
