using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Features.Auth.Services;

[AutoRegister]
public class TokenCleanupJob(AppData context, ILogger<TokenCleanupJob> logger)
{
    public async Task CleanExpiredAsync()
    {
        var cutoff = DateTime.UtcNow;
        var deleted = await context.AccessTokens
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync();

        logger.LogInformation("TokenCleanupJob: deleted {Count} expired tokens", deleted);
    }
}
