using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetTemplate.Features.Auth.Data;

public class AccessTokenConfiguration : IEntityTypeConfiguration<AccessToken>
{
    public void Configure(EntityTypeBuilder<AccessToken> builder)
    {
        builder.Property(t => t.Jti).HasMaxLength(36);

        builder.HasIndex(t => t.Jti).IsUnique();

        builder.HasIndex(t => new { t.UserId, t.UserRole })
            .HasDatabaseName("idx_access_tokens_user_active")
            .HasFilter("\"IsRevoked\" = false");
    }
}
