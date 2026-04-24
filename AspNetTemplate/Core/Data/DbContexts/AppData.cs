using Microsoft.EntityFrameworkCore;
using AspNetTemplate.Core.Data.DbContexts;

namespace AspNetTemplate.Data
{
    public partial class AppData
    {
        public DbSet<UploadedFile> Files { get; set; }
    }
}
