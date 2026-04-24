using AspNetTemplate.Data;
using Serilog;
using Serilog.Events;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class AppBuilderExtensions
    {
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(AppContext.BaseDirectory, AppState.AppData, "Logs",
                        $"app-log-{AppState.Swagger_Doc_Version}-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateBootstrapLogger();

            builder.Logging.ClearProviders();
            builder.Host.UseSerilog((ctx, lc) =>
            {
                if (ctx.HostingEnvironment.IsDevelopment())
                {
                    // DEV: Verbose EF logs, everything logged
                    lc.MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Debug)
                        .WriteTo.Console()
                        .WriteTo.File(
                            Path.Combine(Environment.CurrentDirectory, AppState.AppData, "DebugEFLogs", "log-.log"),
                            rollingInterval: RollingInterval.Day);
                }
                else
                {
                    // PROD: Only important logs
                    lc.MinimumLevel.Warning() // <= only Warning, Error, Fatal
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                        .WriteTo.Console()
                        .WriteTo.File(
                            Path.Combine(AppContext.BaseDirectory, AppState.AppData, "Logs",
                                $"err-log-{AppState.Swagger_Doc_Version}-.log"),
                            rollingInterval: RollingInterval.Day);
                }
            });

            return builder;
        }

        public static IApplicationBuilder UseSimpleAuth(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
