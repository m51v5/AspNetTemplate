using FluentValidation;
using Hangfire;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Helpers;
using AspNetTemplate.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using AspNetTemplate.Core.Endpoints;
using AspNetTemplate.Core.Infra.Filters;
using AspNetTemplate.Data;
using System.Reflection;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class DependencyInjection
    {
        private static readonly System.Text.Json.JsonSerializerOptions HealthCheckJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static IServiceCollection AddServicesFromExtensions(this IServiceCollection services)
        {
            try
            {
                if (!Directory.Exists(AppState.AppData))
                    Directory.CreateDirectory(AppState.AppData);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Couldn't create Directory : \"{AppState.AppData}\"");
            }

            // Ensure private storage directory exists on startup
            try
            {
                var privatePath = Path.Combine(AppContext.BaseDirectory, AppState.PrivatePath);
                if (!Directory.Exists(privatePath))
                    Directory.CreateDirectory(privatePath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Couldn't create private storage directory");
            }

            services.AddRateLimiterConfig();
            services.AddAllowAllCors();
            services.AddAuthConfig();
            services.AddHealthChecksConfig();

            services.AddControllers(opts =>
            {
                opts.Filters.Add<GlobalExceptionFilter>();
                opts.Filters.Add<SoftDeleteAccessFilter>();
                opts.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRouteTransformer()));
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(AppState.Swagger_Document_Name, new OpenApiInfo
                {
                    Title = AppState.Swagger_Doc_AppTitle,
                    Version = AppState.Swagger_Doc_Version,
                    Description = AppState.Swagger_Doc_AppDescription
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header, \"Authorization: Bearer {token}\""
                });
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference("Bearer", document), [] }
                });
                c.SchemaFilter<FluentValidationDescriptionFilter>();
            });

            // Auto-register services decorated with [AutoRegister]
            var assembly = typeof(DependencyInjection).Assembly;
            services.AddValidatorsFromAssembly(assembly);
            foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (var attr in type.GetCustomAttributes<AutoRegisterAttribute>())
                {
                    services.Add(new ServiceDescriptor(attr.ServiceType ?? type, type, attr.Lifetime));
                }
            }

            // Cron Jobs
            services.AddHangfire(config =>
                    config.UsePostgreSqlStorage(options =>
                            options.UseNpgsqlConnection(AppState.C_str_PostgreSql))
                        .UseSerilogLogProvider())
                .AddHangfireServer();

            return services;
        }

        public static WebApplication ExposeUploadsFolder(this WebApplication app)
        {
            // Ensure Uploads folder exists inside Cache
            var uploadsPath = Path.Combine(AppContext.BaseDirectory, AppState.UploadsPath);
            try
            {
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Couldn't create Directory : \"{uploadsPath}\"");
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = $"/{AppState.UploadsUrl}"
            });

            return app;
        }

        public static WebApplication Fallback404InUploadsFolder(this WebApplication app)
        {
            app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments($"/{AppState.UploadsUrl}"),
                uploadsApp =>
                {
                    uploadsApp.Use(async (context, next) =>
                    {
                        await next();

                        // Only fallback if static files failed
                        if (context.Response.StatusCode != StatusCodes.Status404NotFound)
                            return;

                        var uploadsRoot = Path.Combine(
                            AppContext.BaseDirectory,
                            AppState.AppData,
                            AppState.UploadsUrl
                        );

                        var relativePath = context.Request.Path
                            .Value![AppState.UploadsUrl.Length..];

                        var fullPath = Path.GetFullPath(
                            Path.Combine(uploadsRoot, relativePath)
                        );

                        var safeRoot = uploadsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                        if (!fullPath.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                            return; // reject path traversal attempt

                        if (!File.Exists(fullPath))
                            return; // keep 404

                        context.Response.Clear();
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.Headers.ContentDisposition =
                            $"attachment; filename=\"{Path.GetFileName(fullPath)}\"";

                        await context.Response.SendFileAsync(fullPath);
                    });
                });

            return app;
        }

        public static WebApplication ScheduleCleanupJob(this WebApplication app)
        {
            // Schedule cleanup job
            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

                // Schedule your recurring jobs
                recurringJobManager.AddOrUpdate<TokenCleanupJob>("tokens-cleanup",
                    job => job.CleanExpiredAsync(), Cron.Daily);
                recurringJobManager.AddOrUpdate<CleanupOrphanUploadedFiles>("uploaded-files-cleanup",
                    cache => cache.CleanupOrphanedFilesAsync(), Cron.Daily);
            }

            return app;
        }

        public static WebApplication UseBasicConfig(this WebApplication app)
        {
            app.UseCors("AllowAll");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSimpleAuth();

            return app;
        }

        public async static Task<IApplicationBuilder> ApplyPendingMigrationAndSeeding(this WebApplication app)
        {
            // Apply any pending migrations
            // Note : if there is changes that didnt migrate yet, it will throw error (and thats the point of using it actually)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppData>();
                try
                {
                    Log.Information("Migration begin...");
                    await db.Database.MigrateAsync();
                    Log.Information("Migration successful!");
                }
                catch (Exception ex)
                {
                    Log.Error($"Migration failed and rolled back: {ex.Message}");
                    throw;
                }
            }

            await app.UseSeedSuperAdmins();

            return app;
        }

        public async static Task<WebApplication> MapEverything(this WebApplication app)
        {
            // Hangfire dashboard
            app.MapHangfireDashboard("/hangfire", new DashboardOptions());

            app.UseMiddleware<SwaggerBasicAuthMiddleware>();
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{AppState.Swagger_Document_Name}/swagger.json", AppState.Swagger_Doc_AppTitle);
                c.DocumentTitle = AppState.Swagger_Doc_AppTitle ?? "API Docs";
                c.InjectStylesheet("/swagger/swagger-dark.css");
                c.InjectJavascript("/swagger/theme-switcher-and-request-body-filter.js");
                c.DisplayRequestDuration();
                c.EnableFilter();
                c.EnableDeepLinking();
                c.EnablePersistAuthorization();
                c.EnableTryItOutByDefault();
            });

            // Scalar UI
            app.MapScalarApiReference(options =>
            {
                options.Title = AppState.Swagger_Doc_AppTitle;
                options.OpenApiRoutePattern = $"/swagger/{AppState.Swagger_Document_Name}/swagger.json";
            });

            app.MapEnumEndpoints();
            app.MapControllers();

            app.MapHealthChecks("/health-check", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        checkedAt = DateTime.UtcNow,
                        duration = report.TotalDuration,
                        checks = report.Entries.ToDictionary(
                            e => e.Key,
                            e => new
                            {
                                status = e.Value.Status.ToString(),
                                duration = e.Value.Duration,
                                description = e.Value.Description,
                                error = e.Value.Exception?.Message
                            })
                    };

                    await context.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(response, HealthCheckJsonOptions));
                }
            });

            return app;
        }
    }
}
