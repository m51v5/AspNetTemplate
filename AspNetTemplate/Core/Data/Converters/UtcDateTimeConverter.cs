using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetTemplate.Core.Data.Converters
{
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }
}
