using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Data;
using static AspNetTemplate.Data.Enums;

namespace AspNetTemplate.Core.Infra.Attributes
{
    /// <summary>
    /// Validates JWT Bearer token against the database, checks roles, and populates
    /// HttpContext.Items["UserId"], ["User"], ["Role"].
    /// Replaces RoleAuthorizePreProcessor from FastEndpoints.
    /// Usage: [RoleAuthorize(AppRoles.Admin, AppRoles.SuperAdmin)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizeAttribute(params AppRoles[] roles) : Attribute, IFilterFactory
    {
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider services)
            => new RoleAuthorizeFilter(
                services.GetRequiredService<AppData>(),
                roles);
    }

    internal class RoleAuthorizeFilter(AppData db, AppRoles[] allowedRoles) : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            var http = ctx.HttpContext;

            // 1. Read Authorization header
            var authHeader = http.Request.Headers.Authorization.FirstOrDefault();

            if (IsMissingOrInvalid(authHeader, out var token))
            {
                SetUnauthorized(ctx, "Missing or invalid Authorization header.");
                return;
            }

            try
            {
                // 2. Validate JWT signature & claims
                var principal = JwtToken.Validate(token);
                if (principal is null)
                {
                    SetUnauthorized(ctx, "Invalid token.");
                    return;
                }

                http.User = principal;

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!Guid.TryParse(userId, out var userIdGuid) || string.IsNullOrEmpty(jti))
                {
                    SetUnauthorized(ctx, "Invalid token claims.");
                    return;
                }

                // 3. Verify token in DB (not revoked / not expired)
                var access = await db.AccessTokens
                    .Where(t => t.Jti == jti && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (access is null)
                {
                    SetUnauthorized(ctx, "Expired or revoked token.");
                    return;
                }

                // 5. Populate HttpContext.Items
                http.Items["UserId"] = access.UserId;
                http.Items["Role"] = access.UserRole;

                // 6. SuperAdmin bypasses all role restrictions
                //if (principal.HasRole(AppRoles.SuperAdmin))
                //    return;

                // 7. Role check (empty allowedRoles means any authenticated user is allowed)
                if (allowedRoles.Length > 0 && !allowedRoles.Any(r => principal.HasRole(r)))
                {
                    SetForbidden(ctx, "Forbidden action.");
                    return;
                }
            }
            catch
            {
                SetUnauthorized(ctx, "Invalid token, couldn't verify correctly.");
            }
        }

        private static bool IsMissingOrInvalid(string? header, out string token)
        {
            if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer "))
            {
                token = "";
                return true;
            }

            token = header["Bearer ".Length..].Trim();
            return false;
        }

        private static void SetUnauthorized(AuthorizationFilterContext ctx, string message)
            => ctx.Result = new ObjectResult(new FailureResponse(message, HttpStatusCode.Unauthorized))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

        private static void SetForbidden(AuthorizationFilterContext ctx, string message)
            => ctx.Result = new ObjectResult(new FailureResponse(message, HttpStatusCode.Forbidden))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
    }
}
