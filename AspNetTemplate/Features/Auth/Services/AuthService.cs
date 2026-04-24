using System.Net;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Data;
using AspNetTemplate.Features.Auth.Contracts;
using AspNetTemplate.Features.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Features.Auth.Services;

public interface IAuthService
{
    Task<IApiResponse> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<IApiResponse> LogoutAsync(string jti, CancellationToken ct);
    Task<IApiResponse> ResetPasswordAsync(ResetUserPasswordRequest req, CancellationToken ct);
    Task<IApiResponse> ChangeOwnPasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct);
}

[AutoRegister(typeof(IAuthService))]
public class AuthService(AppData context, ILogger<AuthService> logger) : BaseService(context), IAuthService
{
    public async Task<IApiResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == req.Username && !u.IsDeleted, ct);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Login failed: username={Username} reason={Reason}",
                req.Username, user is null ? "not found" : "inactive");
            return new FailureResponse("Invalid credentials.", HttpStatusCode.Unauthorized);
        }

        if (!req.Password.VerifyHashedPassword(user.PasswordHash))
        {
            logger.LogWarning("Login failed: username={Username} reason=wrong password", req.Username);
            return new FailureResponse("Invalid credentials.", HttpStatusCode.Unauthorized);
        }

        var role = user.IsAdmin ? Enums.AppRoles.Admin : Enums.AppRoles.User;
        var (token, jti, exp) = JwtToken.GenToken(user.Id.ToString(), role.ToString(), days: 1);

        _context.AccessTokens.Add(new AccessToken
        {
            Jti       = jti,
            UserId    = user.Id,
            UserRole  = role,
            ExpiresAt = exp,
        });
        await _context.SaveChangesAsync(ct);

        return new SuccessResponse<LoginResponse>(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt   = exp,
            Role        = role.ToString(),
            User        = new LoginUserDto { Id = user.Id, Name = $"{user.FirstName} {user.LastName}" }
        });
    }

    public async Task<IApiResponse> LogoutAsync(string jti, CancellationToken ct)
    {
        var token = await _context.AccessTokens
            .FirstOrDefaultAsync(t => t.Jti == jti && !t.IsRevoked, ct);

        if (token is not null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync(ct);
        }

        return new EmptySuccessResponse();
    }

    public async Task<IApiResponse> ResetPasswordAsync(ResetUserPasswordRequest req, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == req.UserId && !u.IsDeleted, ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        user.PasswordHash = req.NewPassword.AsHashedPassword();
        await _context.SaveChangesAsync(ct);

        logger.LogInformation("Admin reset password for userId={UserId}", req.UserId);
        return new EmptySuccessResponse();
    }

    public async Task<IApiResponse> ChangeOwnPasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

        if (user is null)
            return new FailureResponse("User not found.", HttpStatusCode.NotFound);

        if (!req.CurrentPassword.VerifyHashedPassword(user.PasswordHash))
            return new FailureResponse("Current password is incorrect.", HttpStatusCode.BadRequest);

        user.PasswordHash = req.NewPassword.AsHashedPassword();
        await _context.SaveChangesAsync(ct);

        return new EmptySuccessResponse();
    }
}
