using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data.Common;
using System.Net;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Core.Infra.Extensions;

namespace AspNetTemplate.Core.Infra.Filters
{
    public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IErrorEventCollector errorEvents) : IAsyncExceptionFilter
    {
        public async Task OnExceptionAsync(ExceptionContext ctx)
        {
            var errorId = GuidV7.NewGuid();

            // TODO: how the errorEvents are being used in this case ?
            await errorEvents.InvokeAllAsync();

            if (ctx.Exception is DbException dbEx)
            {
                logger.LogError(dbEx, "[{ErrorId}] Database error occurred.", errorId);
                ctx.Result = new ObjectResult(
                    new FailureResponse($"فشل حفظ البيانات, معرف الخطأ '{errorId}'.", HttpStatusCode.BadRequest))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            else
            {
                logger.LogError(ctx.Exception, "[{ErrorId}] Internal server error occurred.", errorId);
                ctx.Result = new ObjectResult(
                    new FailureResponse($"فشل معالجة الطلب, معرف الخطأ '{errorId}'.", HttpStatusCode.InternalServerError))
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            ctx.ExceptionHandled = true;
        }
    }
}
