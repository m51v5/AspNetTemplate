using System.Security.Claims;
using static AspNetTemplate.Data.Enums;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return HasRole(user, AppRoles.Admin);
        }

        public static bool HasRole(this ClaimsPrincipal user, params AppRoles[] roles)
        {
            List<ClaimsIdentity> ids = [.. user.Identities];
            if (roles == null || roles.Length == 0 || ids.Count == 0)
                return false;

            // HashSet for O(1) lookup
            HashSet<string>? roleSet = new(roles.Select(r => r.ToString()));

            for (int i = 0; i < ids.Count; i++)
            {
                ClaimsIdentity? identity = ids[i];
                if (identity == null)
                    continue;

                foreach (var claim in identity.Claims)
                {
                    if (claim.Type == identity.RoleClaimType)
                    {
                        if (roleSet.Contains(claim.Value))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static Guid GetCurrentUserIdOrEmpty(this ClaimsPrincipal user)
        {
            return GetCurrentUserId(user) ?? Guid.Empty;
        }

        public static Guid? GetCurrentUserId(this ClaimsPrincipal user)
        {
            if (user == null) return null;

            // Assuming user ID is stored as a claim in the JWT token
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return null;

            return userId;
        }
    }
}