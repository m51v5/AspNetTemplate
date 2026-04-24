using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.Converters;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AspNetTemplate.Data
{
    public partial class AppData(DbContextOptions<AppData> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyOnDeleteBehaviorFromAttributes();
            modelBuilder.ApplyDbTriggers();
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            FileUsageTriggerGuard.Validate(modelBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>()
                .HaveColumnType("timestamptz");
        }

    }
}
