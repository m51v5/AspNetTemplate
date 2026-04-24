using AspNetTemplate.Core.Data.DbContexts.Interceptors;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Data;
using Laraue.EfCoreTriggers.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
}

AppState.Config = builder.Configuration;

// Allow up to 200 MB request bodies globally
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024;
});

builder.AddSerilogLogging();

Log.Information("Starting...");

builder.Services.AddServicesFromExtensions();
builder.Services.AddHttpClient();


// Move the process-level switch outside of the per-context options delegate
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppData>(options =>
{
    options.AddInterceptors(new SlowQueryInterceptor());
    options.AddInterceptors(new DateTimeInterceptors());

    options.UseNpgsql(AppState.C_str_PostgreSql)
        .UsePostgreSqlTriggers();

    if (builder.Environment.IsDevelopment())
    {
        options.UseLoggerFactory(new SerilogLoggerFactory(Log.Logger))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
}); // Scoped is the default — one context per HTTP request, one per Hangfire job execution

#region app

var app = builder.Build();

app.ExposeUploadsFolder();
app.Fallback404InUploadsFolder();
app.UseBasicConfig();

await app.ApplyPendingMigrationAndSeeding();
app.ScheduleCleanupJob();
await app.MapEverything();

app.Lifetime.ApplicationStarted.Register(() =>
    Log.Information("Running on: {Urls}", string.Join(", ", app.Urls)));

await app.RunAsync();

#endregion