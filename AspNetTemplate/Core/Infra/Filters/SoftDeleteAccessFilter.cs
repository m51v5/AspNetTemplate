using AspNetTemplate.Core.Data.Base;
using Microsoft.AspNetCore.Mvc.Filters;
using static AspNetTemplate.Data.Enums;

namespace AspNetTemplate.Core.Infra.Filters
{
    /// <summary>
    /// Forces IsDeleted = false on any BaseSoftFilter parameter for non-SuperAdmin users.
    /// Prevents regular users from querying soft-deleted records even if they pass isDeleted=true.
    /// SuperAdmin passes through untouched.
    /// </summary>
    public sealed class SoftDeleteAccessFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            //if (context.HttpContext.User.IsInRole(AppRoles.SuperAdmin.ToString()))
            //    return;

            foreach (var arg in context.ActionArguments.Values)
            {
                if (arg is BaseSoftFilter filter)
                    filter.IsDeleted = false;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
