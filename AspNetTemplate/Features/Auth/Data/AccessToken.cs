using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Data;

namespace AspNetTemplate.Features.Auth.Data;

public class AccessToken : BaseEntity
{
    public required string Jti { get; set; }

    // Polymorphic — no DB-level FK (spans admins, doctors, lab_techs)
    public Guid UserId { get; set; }
    public Enums.AppRoles UserRole { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
}
