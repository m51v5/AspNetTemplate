using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Features.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Data
{
    public static class Seeder
    {
        private static readonly Guid AdminUserId = GuidV7.NewGuid();
        private static readonly Guid RegularUserId = GuidV7.NewGuid();

        private const string DefaultPassword = "Password@123";

        public static async Task<IApplicationBuilder> UseSeedSuperAdmins(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppData>();

            await SeedAdminUser(db);
            await SeedRegularUser(db);

            return app;
        }

        private static async Task SeedAdminUser(AppData db)
        {
            if (await db.Users.AnyAsync()) return;

            db.Users.Add(new User
            {
                Id           = AdminUserId,
                FirstName    = "Super",
                LastName     = "Admin",
                Username     = "admin",
                Email        = AppState.SuperAdminEmail,
                PasswordHash = AppState.SuperAdminPassword,
                IsActive     = true,
                IsAdmin      = true,
            });
            await db.SaveChangesAsync();
        }

        private static async Task SeedRegularUser(AppData db)
        {
            if (await db.Users.AnyAsync()) return;

            db.Users.Add(new User
            {
                Id           = RegularUserId,
                FirstName    = "John",
                LastName     = "Doe",
                Username     = "john.doe",
                Email        = "john.doe@example.com",
                PasswordHash = DefaultPassword.AsHashedPassword(),
                IsActive     = true,
            });
            await db.SaveChangesAsync();
        }
    }
}
