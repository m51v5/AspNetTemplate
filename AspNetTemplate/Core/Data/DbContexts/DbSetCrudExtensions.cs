using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AspNetTemplate.Core.Data.Base;

namespace AspNetTemplate.Core.Data.DbContexts
{
    public static class DbSetCrudExtensions
    {
        #region Get

        /// <summary>
        ///     Get a paged list of entities projected to light DTOs with filtering.
        /// </summary>
        public static async Task<PagedList<TLightDto>> GetAllAsync<TEntity, TFilterDto, TLightDto>(
            this DbSet<TEntity> dbSet,
            TFilterDto filter,
            Expression<Func<TEntity, TLightDto>> selector,
            IQueryable<TEntity>? initQuery = null,
            Expression<Func<TEntity, bool>>? exFilter = null,
            CancellationToken ct = default
        )
            where TEntity : BaseEntity
            where TFilterDto : BaseFilter<TEntity>
        {
            var query = filter
                .ApplyFilters(initQuery ?? dbSet.AsQueryable());

            if (exFilter != null)
                query = query.Where(exFilter);

            query = query.AsNoTracking().Distinct();

            return await PagedList<TLightDto>.Create(
                query.Select(selector), filter.PageNumber, filter.PageSize, ct);
        }

        /// <summary>
        ///     Get a single entity by Id with detailed selector.
        /// </summary>
        public static async Task<TDetailedDto?> GetByIdAsync<TEntity, TDetailedDto>(
                this DbSet<TEntity> dbSet,
                Guid id,
                Expression<Func<TEntity, TDetailedDto>> selector,
                Expression<Func<TEntity, bool>>? exFilter = null,
                CancellationToken ct = default
            ) where TEntity : BaseEntity
        {
            var query = dbSet.Where(e => e.Id == id).AsQueryable();

            if (exFilter != null)
                query = query.Where(exFilter);

            return await query.Select(selector).FirstOrDefaultAsync(ct);
        }

        /// <summary>
        ///     Get a single soft-deletable entity by Id with detailed selector.
        /// </summary>
        public static async Task<TDetailedDto?> GetByIdAsync<TEntity, TDetailedDto>(
                this DbSet<TEntity> dbSet,
                Guid id,
                Expression<Func<TEntity, TDetailedDto>> selector,
                bool? isDeleted = null,
                Expression<Func<TEntity, bool>>? exFilter = null,
                CancellationToken ct = default
            ) where TEntity : BaseSoftEntity
        {
            var query = dbSet.Where(e => e.Id == id);

            if (isDeleted.HasValue)
                query = query.Where(e => !e.IsDeleted || isDeleted.Value);

            if (exFilter != null)
                query = query.Where(exFilter);

            return await query.Select(selector).FirstOrDefaultAsync(ct);
        }

        #endregion

        #region Create / Update

        /// <summary>
        /// Create a new entity from form, return detailed DTO.
        /// </summary>
        public static async Task<TDetailedDto?> CreateAsync<TEntity, TCreateDto, TDetailedDto>(
                this DbSet<TEntity> dbSet,
                DbContext context,
                TCreateDto form,
                Expression<Func<TEntity, TDetailedDto>> selector,
                CancellationToken ct = default
            ) where TEntity : BaseEntity where TCreateDto : IBaseCreateDto<TEntity>
        {
            return selector.Compile()(await CreateAsync(dbSet, context, form, ct));
        }

        public static async Task<TEntity> CreateAsync<TEntity, TCreateDto>(
                this DbSet<TEntity> dbSet,
                DbContext context,
                TCreateDto form,
                CancellationToken ct = default
            ) where TEntity : BaseEntity where TCreateDto : IBaseCreateDto<TEntity>
        {
            var entity = await form.CreateRecord();

            await dbSet.AddAsync(entity, ct);
            await context.SaveChangesAsync(ct);

            return entity;
        }

        /// <summary>
        /// Update an existing entity by Id using form, return detailed DTO.
        /// </summary>
        public static async Task<TDetailedDto?> UpdateAsync<TEntity, TUpdateDto, TDetailedDto>(
            this DbSet<TEntity> dbSet,
            DbContext context,
            Guid id,
            TUpdateDto form,
            Expression<Func<TEntity, TDetailedDto>> selector,
            CancellationToken ct = default
        ) where TEntity : BaseEntity where TUpdateDto : IBaseUpdateDto<TEntity>
        {
            var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (entity == null) return default;

            await form.UpdateRecord(entity);
            await context.SaveChangesAsync(ct);

            return selector.Compile()(entity);
        }

        #endregion

        #region Delete / Soft / Restore

        /// <summary>
        /// Delete an entity by Id.
        /// </summary>
        public static async Task<bool> DeleteAsync<TEntity>(
                this DbSet<TEntity> dbSet,
                DbContext context,
                Guid id,
                CancellationToken ct = default
            ) where TEntity : BaseSoftEntity
        {
            var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (entity == null)
                return false;

            dbSet.Remove(entity);
            await context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Soft-delete an entity by Id.
        /// </summary>
        public static async Task<bool> SoftDeleteAsync<TEntity>(
                this DbSet<TEntity> dbSet,
                DbContext context,
                Guid id,
                CancellationToken ct = default
            ) where TEntity : BaseSoftEntity
        {
            var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);
            if (entity == null)
                return false;

            await entity.SoftDelete();
            await context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        ///     Restore a previously soft-deleted entity by Id.
        /// </summary>
        public static async Task<bool> RestoreSoftDeletedAsync<TEntity>(
                this DbSet<TEntity> dbSet,
                DbContext context,
                Guid id,
                CancellationToken ct = default
            ) where TEntity : BaseSoftEntity
        {
            var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, ct);
            if (entity == null)
                return false;

            await entity.UnSoftDelete();
            await context.SaveChangesAsync(ct);

            return true;
        }

        #endregion
    }
}
