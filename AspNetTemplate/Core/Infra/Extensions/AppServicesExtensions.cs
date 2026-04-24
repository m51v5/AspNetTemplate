using AspNetTemplate.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class AppServicesExtensions
    {
        public static IServiceCollection AddAuthConfig(this IServiceCollection services)
        {
            string? jwtSignInKey = AppState.Config?.GetSection("Jwt:SecretKey").Get<string>();
            if (jwtSignInKey == null)
            {
                throw new ArgumentNullException("Jwt Secret Key not set or failed to load to the api!");
            }
            if (Encoding.UTF8.GetByteCount(jwtSignInKey) < 64)
            {
                throw new InvalidOperationException("JWT secret key must be at least 64 bytes.");
            }

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AppState.App_domain,
                        ValidateAudience = true,
                        ValidAudience = AppState.App_domain,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSignInKey))
                    };

                    // Optional: handle unauthorized responses
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrEmpty(context.Token))
                            {
                                var claims = new[] { new Claim(ClaimTypes.Name, "Anonymous") };
                                var identity = new ClaimsIdentity(claims, "Bearer");
                                context.Principal = new ClaimsPrincipal(identity);
                                context.Success();
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddAllowAllCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            return services;
        }

        public static IServiceCollection AddRateLimiterConfig(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context =>
                {
                    IPAddress? ipAddress = context.Connection.RemoteIpAddress;
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                    return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100, // Allow 100 requests
                            Window = TimeSpan.FromMinutes(1), // Per 1 minute window
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
                });
            });

            return services;
        }

        public static IServiceCollection AddHealthChecksConfig(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<AppData>("database");

            return services;
        }
    }

public class AcceptHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            // NOTE : Breaking Changes due to upgrade to .NET 10 and latest Swashbuckle
            // Add the Accept-Language header parameter
            //operation.Parameters.Add(new OpenApiParameter
            //{
            //    Name = "Accept",
            //    In = ParameterLocation.Header,
            //    Required = false,
            //    Schema = new OpenApiSchema
            //    {
            //        Type = "string",
            //        Enum = new List<IOpenApiAny>()
            //        {
            //            new OpenApiString("application/json"),
            //            new OpenApiString("application/xml"),
            //            new OpenApiString("text/plain"),
            //        },
            //        Default = new OpenApiString("application/json")
            //    }
            //});
        }
    }
}