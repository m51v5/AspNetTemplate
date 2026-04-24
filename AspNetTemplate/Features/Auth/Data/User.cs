using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.DbContexts;

namespace AspNetTemplate.Features.Auth.Data;

public class User : BaseSoftEntity, IHasOptionalFile
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    // IHasOptionalFile — profile photo
    public Guid? FileId { get; set; }
    public UploadedFile? File { get; set; }
}
