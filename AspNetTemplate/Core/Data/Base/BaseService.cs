using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Data.Base;

public abstract class BaseService(AppData context)
{
    protected readonly AppData _context = context;
}
