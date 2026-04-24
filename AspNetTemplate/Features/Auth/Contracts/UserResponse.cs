using System.Linq.Expressions;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Data;
using AspNetTemplate.Features.Auth.Data;

namespace AspNetTemplate.Features.Auth.Contracts;

public class UserResponse : BaseSoftResponseDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public string? ImageUrl { get; set; }

    public UserResponse(User user) : base(user)
    {
        FirstName = user.FirstName;
        LastName  = user.LastName;
        Username  = user.Username;
        Email     = user.Email;
        IsActive  = user.IsActive;
        IsAdmin   = user.IsAdmin;
        ImageUrl  = user.File is not null
            ? AppState.UploadsDomain + user.File.FolderPath + "/" + user.FileId + user.File.Extension
            : null;
    }

    public UserResponse() { }

    public static Expression<Func<User, UserResponse>> Projection =>
        u => new UserResponse
        {
            Id        = u.Id,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            IsDeleted = u.IsDeleted,
            DeletedAt = u.DeletedAt,
            FirstName = u.FirstName,
            LastName  = u.LastName,
            Username  = u.Username,
            Email     = u.Email,
            IsActive  = u.IsActive,
            IsAdmin   = u.IsAdmin,
            ImageUrl  = u.File != null
                ? AppState.UploadsDomain + u.File.FolderPath + "/" + u.FileId.ToString() + u.File.Extension
                : null
        };
}
