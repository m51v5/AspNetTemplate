namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class GuidExtensions
    {
        /// <summary>
        /// Returns true if the Guid is null or equal to Guid.Empty (all zeros).
        /// Useful for guarding against frontends sending "00000000-0000-0000-0000-000000000000"
        /// instead of null for optional FK fields.
        /// </summary>
        public static bool IsNullOrEmpty(this Guid? id)
            => id == null || id == Guid.Empty;
    }

    public static class GuidV7
    {
        /// <summary>
        /// Creates a new version 7 GUID (time-ordered).
        /// Prefer this over <see cref="Guid.NewGuid"/> for entity IDs and file names —
        /// sequential inserts avoid DB index page splits and files sort chronologically.
        /// </summary>
        public static Guid NewGuid() => Guid.CreateVersion7();
    }
}
