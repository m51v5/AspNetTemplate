using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using AspNetTemplate.Core.Data.Base;

namespace AspNetTemplate.Core.Data.DbContexts.Interceptors
{
    public class DateTimeInterceptors : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            foreach (var entry in eventData.Context.ChangeTracker.Entries())
            {

                if (entry is { State: EntityState.Added, Entity: BaseEntity createdEntity })
                {
                    var now = DateTime.UtcNow;
                    createdEntity.CreatedAt = now;
                    createdEntity.UpdatedAt = now;
                }
                else if (entry is { State: EntityState.Modified, Entity: BaseEntity updatedEntity })
                {
                    updatedEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
