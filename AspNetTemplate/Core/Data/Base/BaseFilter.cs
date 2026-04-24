using System.Security.Claims;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Data.Base
{
    public class BaseFilter : BaseFilter<BaseEntity>
    {
    }

    public class BaseSoftFilter : BaseSoftFilter<BaseSoftEntity>
    {
    }

    public abstract class BaseFilter<TEntity>
        where TEntity : BaseEntity
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 30;
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }

        public virtual IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query)
        {
            return query.WhereBetween(CreatedAfter, CreatedBefore, x => x.CreatedAt);
        }
    }

    public abstract class BaseSoftFilter<TEntity> : BaseFilter<TEntity>
        where TEntity : BaseSoftEntity
    {
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAfter { get; set; }
        public DateTime? DeletedBefore { get; set; }

        public override IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query)
        {
            query = base.ApplyFilters(query);

            if (IsDeleted.HasValue)
                query = query.Where(e => e.IsDeleted == IsDeleted.Value);

            return query.WhereBetween(DeletedAfter, DeletedBefore, x => x.DeletedAt);
        }
    }
}
