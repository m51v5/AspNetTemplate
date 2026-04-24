using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Features.Auth.Data;

namespace AspNetTemplate.Features.Auth.Contracts;

public class UserFilter : BaseSoftFilter<User>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }

    public override IQueryable<User> ApplyFilters(IQueryable<User> query)
    {
        query = base.ApplyFilters(query);

        query = query
            .WhereLike(u => u.FirstName, FirstName)
            .WhereLike(u => u.LastName,  LastName)
            .WhereLike(u => u.Username,  Username)
            .WhereLike(u => u.Email,     Email);

        return query;
    }
}
