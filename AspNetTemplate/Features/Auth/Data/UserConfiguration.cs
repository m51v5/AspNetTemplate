using AspNetTemplate.Core.Data.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetTemplate.Features.Auth.Data;

public class UserConfiguration : OptionalFileUsageTriggerConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.Username).HasMaxLength(50);
        builder.Property(u => u.Email).HasMaxLength(320);

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        // Only one user may hold IsAdmin = true at a time (the super-admin seed).
        // Remove this constraint if your project allows multiple admins.
        builder.HasIndex(u => u.IsAdmin)
            .HasFilter("\"IsAdmin\" = TRUE")
            .IsUnique();
    }
}
