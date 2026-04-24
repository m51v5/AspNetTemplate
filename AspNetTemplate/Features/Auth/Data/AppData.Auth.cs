using AspNetTemplate.Features.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Data;

public partial class AppData
{
    public DbSet<User> Users { get; set; }
    public DbSet<AccessToken> AccessTokens { get; set; }
}
