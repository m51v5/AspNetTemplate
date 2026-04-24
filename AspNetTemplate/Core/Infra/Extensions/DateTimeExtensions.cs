namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts a DateTime to DateOnly, stripping the time component.
        /// Use for birth dates, schedule dates, and any date-only domain values.
        /// </summary>
        public static DateOnly ToDateOnly(this DateTime dateTime)
            => DateOnly.FromDateTime(dateTime);
    }
}
