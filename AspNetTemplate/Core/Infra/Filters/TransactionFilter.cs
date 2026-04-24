using AspNetTemplate.Core.Infra.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Infra.Filters
{
    // TODO: why there is 2 classes ? why not making them one ?

    /// <summary>
    /// Wraps the action in an EF Core database transaction.
    /// Commits on success, rolls back on any exception.
    /// Replaces SafeEndpointWithDb transaction behavior.
    /// </summary>
    [AutoRegister]
    public class TransactionFilter(AppData db) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var result = await next();

            if (result.Exception is null || result.ExceptionHandled)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Apply on a controller class or action method to wrap it in a DB transaction.
    /// Usage: [UseTransaction]
    /// </summary>
    public class UseTransactionAttribute : ServiceFilterAttribute
    {
        public UseTransactionAttribute() : base(typeof(TransactionFilter)) { }
    }
}
