using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Infra.Helpers
{
    /// <summary>
    /// Static wrapper around FileHelper for public user uploads (profile photos, etc.).
    /// Files are saved under AppState.UploadsPath and served via static files middleware.
    /// For private/protected files use a feature-specific service.
    /// </summary>
    public static class UploadHelper
    {
        public static readonly string RootPath = Path.Combine(AppContext.BaseDirectory, AppState.UploadsPath);

        public static Task<(bool success, UploadedFile? file, string? error)> SaveFileAsync(
            IFormFile file,
            string folderName,
            string[]? allowedExtensions = null,
            long maxFileSizeMegaBytes = 5)
            => FileHelper.SaveFileAsync(RootPath, file, folderName, allowedExtensions, maxFileSizeMegaBytes);

        public static (bool success, string? filePath, string? contentType, string? error) GetFile(
            string folderName, string fileName)
            => FileHelper.GetFile(RootPath, folderName, fileName);

        public static Task CleanupFile(ILogger logger, string? filePath)
            => FileHelper.CleanupFile(RootPath, logger, filePath);

        public static Task<string?> TryUploadOptionalAsync<TEntity>(
            IErrorEventCollector errorEvents,
            ILogger logger,
            AppData db,
            TEntity entity,
            IFormFile? uploadedFile,
            string folderName,
            CancellationToken ct)
            where TEntity : BaseEntity, IHasOptionalFile
            => FileHelper.TryUploadOptionalAsync(RootPath, errorEvents, logger, db, entity, uploadedFile, folderName, ct);

        public static Task<string?> TryUploadAsync<TEntity>(
            IErrorEventCollector errorEvents,
            ILogger logger,
            AppData db,
            TEntity entity,
            IFormFile? uploadedFile,
            string folderName,
            CancellationToken ct)
            where TEntity : BaseEntity, IHasFile
            => FileHelper.TryUploadAsync(RootPath, errorEvents, logger, db, entity, uploadedFile, folderName, ct);
    }
}
