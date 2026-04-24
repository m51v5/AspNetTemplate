using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FluentValidation;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Data;
using AspNetTemplate.Features.Auth.Contracts;
using AspNetTemplate.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetTemplate.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IUserService userService,
    IValidator<LoginRequest> loginValidator,
    IValidator<ResetUserPasswordRequest> resetPasswordValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(SuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(new FailureResponse(validation.ToString(), HttpStatusCode.BadRequest));

        var result = await authService.LoginAsync(req, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("logout")]
    [RoleAuthorize(Enums.AppRoles.Admin, Enums.AppRoles.User)]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
            return Unauthorized(new FailureResponse("Invalid token.", HttpStatusCode.Unauthorized));

        var result = await authService.LogoutAsync(jti, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("profile")]
    [RoleAuthorize(Enums.AppRoles.Admin, Enums.AppRoles.User)]
    [ProducesResponseType(typeof(SuccessResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = User.GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new FailureResponse("Invalid token.", HttpStatusCode.Unauthorized));

        var result = await userService.GetByIdAsync(userId.Value, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("change-password")]
    [RoleAuthorize(Enums.AppRoles.Admin, Enums.AppRoles.User)]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var validation = await changePasswordValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(new FailureResponse(validation.ToString(), HttpStatusCode.BadRequest));

        var userId = User.GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new FailureResponse("Invalid token.", HttpStatusCode.Unauthorized));

        var result = await authService.ChangeOwnPasswordAsync(userId.Value, req, ct);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("reset-password")]
    [RoleAuthorize(Enums.AppRoles.Admin)]
    [ProducesResponseType(typeof(EmptySuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FailureResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetUserPasswordRequest req, CancellationToken ct)
    {
        var validation = await resetPasswordValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(new FailureResponse(validation.ToString(), HttpStatusCode.BadRequest));

        var result = await authService.ResetPasswordAsync(req, ct);
        return StatusCode((int)result.StatusCode, result);
    }
}
