using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Core.Data.DbContexts
{
    public class PagedList<T>(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        [JsonIgnore] public const int maxmunmPageSize = 50;
        public IReadOnlyCollection<T> Items { get; } = items;
        public int PageNumber { get; set; } = pageNumber;
        public int PageSize { get; set; } = pageSize;
        public int TotalCount { get; set; } = totalCount;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static async Task<PagedList<T>> Create(IQueryable<T> query, int? pageNumber, int? pageSize, CancellationToken ct)
        {
            pageNumber = (pageNumber == null || pageNumber < 1) ? 1 : pageNumber;
            pageSize = (pageSize == null || pageSize < 1) ? 20 : pageSize > maxmunmPageSize ? maxmunmPageSize : pageSize;

            int totalCount = await query.CountAsync(ct);
            var items = await query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync(ct);

            return new PagedList<T>(items, pageNumber.Value, pageSize.Value, totalCount);
        }
    }
}
