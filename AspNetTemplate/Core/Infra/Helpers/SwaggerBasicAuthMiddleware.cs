using System.Net.Http.Headers;
using System.Net;
using System.Text;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Infra.Helpers
{
    public class SwaggerBasicAuthMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger")
                || context.Request.Path.StartsWithSegments("/scalar"))
            {
                string? authHeader = context.Request.Headers.Authorization;
                if (authHeader?.StartsWith("Basic ") == true)
                {
                    // Get the credentials from request header
                    var header = AuthenticationHeaderValue.Parse(authHeader);
                    var inBytes = Convert.FromBase64String(s: header.Parameter ?? string.Empty);
                    var credentials = Encoding.UTF8.GetString(inBytes).Split(':');
                    var username = credentials[0];
                    var password = credentials[1];

                    // validate credentials
                    if (username.Equals(AppState.Swagger_UserName)
                      && password.Equals(AppState.Swagger_Password))
                    {
                        await next.Invoke(context).ConfigureAwait(false);
                        return;
                    }
                }
                context.Response.Headers.WWWAuthenticate = "Basic";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                await next.Invoke(context).ConfigureAwait(false);
            }
        }
    }
}
