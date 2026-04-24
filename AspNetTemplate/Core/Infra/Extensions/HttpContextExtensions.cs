using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class HttpContextExtensions
    {
        public static BaseEntity? GetUserAsBase(this HttpContext ctx)
            => ctx.Items["User"] as BaseSoftEntity;

        public static Guid? GetUserId(this HttpContext ctx)
            => ctx.Items["UserId"] as Guid?;

        public static Enums.AppRoles? GetUserRole(this HttpContext ctx)
            => (Enums.AppRoles?)ctx.Items["Role"];
    }
}
