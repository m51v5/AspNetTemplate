namespace AspNetTemplate.Core.Data.Base
{
    public class BaseResponseDto
    {
        public Guid? Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public BaseResponseDto(BaseEntity entity)
        {
            Id = entity.Id;
            CreatedAt = entity.CreatedAt;
            UpdatedAt = entity.UpdatedAt;
        }

        public BaseResponseDto() { }
    }

    public class BaseSoftResponseDto : BaseResponseDto
    {
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public BaseSoftResponseDto(BaseSoftEntity entity) : base(entity)
        {
            IsDeleted = entity.IsDeleted;
            DeletedAt = entity.DeletedAt;
        }

        public BaseSoftResponseDto() { }
    }
}
