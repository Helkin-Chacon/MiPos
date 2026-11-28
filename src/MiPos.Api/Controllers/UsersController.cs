using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users/me")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var profile = await users.GetCurrentUserAsync(cancellationToken);
        return Ok(new ApiResponse<UserProfileResponse> { Data = profile });
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateProfileAsync(request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await users.ChangePasswordAsync(request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }
}
