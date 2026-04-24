using Microsoft.EntityFrameworkCore;
using System.Reflection;
using AspNetTemplate.Core.Data.DbContexts;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyOnDeleteBehaviorFromAttributes(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var properties = clrType.GetProperties();

                foreach (var property in properties)
                {
                    var attr = property.GetCustomAttribute<OnDeleteBehaviorAttribute>();
                    if (attr != null)
                    {
                        var navigation = entityType.FindNavigation(property.Name);
                        if (navigation != null)
                        {
                            var fk = navigation.ForeignKey;
                            fk.DeleteBehavior = attr.DeleteBehavior;
                        }
                    }
                }
            }
        }
    }
}
