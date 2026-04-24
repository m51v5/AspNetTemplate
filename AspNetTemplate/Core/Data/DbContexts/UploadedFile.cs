using AspNetTemplate.Core.Data.Base;

namespace AspNetTemplate.Core.Data.DbContexts
{
    public class UploadedFile : BaseEntity
    {
        // Parameterless ctor required by EF Core for materialization and by Laraue trigger expressions.
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public UploadedFile() { Extension = string.Empty; FolderPath = string.Empty; StoragePath = string.Empty; }

        public required string Extension { get; set; }
        public required string FolderPath { get; set; }

        /// <summary>
        /// Absolute root path where this file was saved (set at upload time by FileHelper).
        /// Used by CleanupOrphanUploadedFiles to locate the physical file regardless of which
        /// </summary>
        public required string StoragePath { get; set; }

        /// <summary>
        /// Incremented/decremented by DB triggers on every entity that implements IHasOptionalFile.
        /// Files with UsageCount = 0 are eligible for cleanup by CleanupOrphanUploadedFiles.
        /// </summary>
        public int UsageCount { get; set; } = 0;


        // Examples
        //public string FullPath => Path.Combine(FolderPath, Id + Extension);
        //public string FullUrl => e.File != null
        //          ? AppState.UploadsDomain + e.File.FolderPath + "/" + e.FileId + e.File.Extension
        //        : null;
    }
}
