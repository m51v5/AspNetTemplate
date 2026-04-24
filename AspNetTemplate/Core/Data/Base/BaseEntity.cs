using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Extensions;

namespace AspNetTemplate.Core.Data.Base
{
    public interface IHasOptionalFile
    {
        Guid? FileId { get; set; }
        UploadedFile? File { get; set; }
    }

    public interface IHasFile
    {
        Guid FileId { get; set; }
        UploadedFile? File { get; set; }
    }


    public interface IBaseCreateDto<TEntity>
        where TEntity : BaseEntity
    {
        Task<TEntity> CreateRecord();
    }

    public interface IBaseUpdateDto<TEntity>
        where TEntity : BaseEntity
    {
        Task UpdateRecord(TEntity entity);
    }

    [Index(nameof(CreatedAt))]
    public class BaseEntity
    {
        [Key] public Guid Id { get; set; } = GuidV7.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    public class BaseSoftEntity : BaseEntity
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Task SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;

            return Task.CompletedTask;
        }
        public Task UnSoftDelete()
        {
            IsDeleted = false;
            DeletedAt = null;

            return Task.CompletedTask;
        }
    }
}
