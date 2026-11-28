using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Requests;
using MiPos.Application.Identity.Responses;

namespace MiPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsers(CancellationToken cancellationToken)
    {
        var result = await users.GetUsersAsync(cancellationToken);
        return Ok(new ApiResponse<List<UserResponse>> { Data = result });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateUserAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserResponse> { Data = user });
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUser(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateUserAsync(userId, request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }

    [HttpPatch("{userId:guid}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserStatus(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateUserStatusAsync(userId, request.IsActive!.Value, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var profile = await users.GetCurrentUserAsync(cancellationToken);
        return Ok(new ApiResponse<UserProfileResponse> { Data = profile });
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateProfileAsync(request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await users.ChangePasswordAsync(request, cancellationToken);
        return Ok(new ApiResponse<bool> { Data = true });
    }
}
