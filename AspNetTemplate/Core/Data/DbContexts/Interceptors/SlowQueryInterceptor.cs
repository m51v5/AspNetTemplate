using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using System.Data.Common;
using System.Diagnostics;

namespace AspNetTemplate.Core.Data.DbContexts.Interceptors
{
    public class SlowQueryInterceptor : DbCommandInterceptor
    {
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var res = await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            sw.Stop();

            if (sw.ElapsedMilliseconds > 3000)
            {
                Log.Warning("SLOW QUERY ({Duration} ms): {Sql}",
                    sw.ElapsedMilliseconds, command.CommandText);
            }

            return res;
        }
    }

}
