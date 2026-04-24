using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Data;

namespace AspNetTemplate.Core.Infra.Helpers
{
    /// <summary>
    /// Core file I/O engine. All methods take an explicit rootPath so callers control
    /// where files land. Use UploadHelper for public uploads or define a feature-specific
    /// </summary>
    public static class FileHelper
    {
        public static async Task<(bool success, UploadedFile? file, string? error)> SaveFileAsync(
            string rootPath,
            IFormFile file,
            string folderName,
            string[]? allowedExtensions = null,
            long maxFileSizeMegaBytes = 5)
        {
            if (file == null || file.Length == 0)
                return (false, null, "No file uploaded");

            if (file.Length > maxFileSizeMegaBytes * 1024 * 1024)
                return (false, null, $"File exceeds maximum size of {maxFileSizeMegaBytes}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (allowedExtensions != null && !Array.Exists(allowedExtensions, e => e == ext))
                return (false, null, "File type not allowed");

            var fileId = GuidV7.NewGuid();
            folderName = folderName.Trim('/');
            var folderPath = Path.Combine(rootPath, folderName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{fileId}{ext}";
            var filePath = Path.Combine(folderPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, new UploadedFile
            {
                Id = fileId,
                Extension = ext,
                FolderPath = folderName,
                StoragePath = rootPath
            }, null);
        }

        public static (bool success, string? filePath, string? contentType, string? error) GetFile(
            string rootPath,
            string folderName,
            string fileName)
        {
            var filePath = Path.Combine(rootPath, folderName, fileName);

            if (!File.Exists(filePath))
                return (false, null, null, "File not found");

            var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return (true, filePath, contentType, null);
        }

        public static async Task CleanupFile(string rootPath, ILogger logger, string? filePath)
        {
            if (filePath.IsUnValidString()) return;

            try
            {
                var absolutePath = Path.Combine(rootPath, filePath!);
                if (File.Exists(absolutePath)) File.Delete(absolutePath);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(ex, "Failed to delete file '{FilePath}' during cleanup", filePath);
            }
        }

        public static async Task<string?> TryUploadOptionalAsync<TEntity>(
            string rootPath,
            IErrorEventCollector errorEvents,
            ILogger logger,
            AppData db,
            TEntity entity,
            IFormFile? uploadedFile,
            string folderName,
            CancellationToken ct)
            where TEntity : BaseEntity, IHasOptionalFile
        {
            if (uploadedFile == null || uploadedFile.Length == 0) return null;

            var (success, file, error) = await SaveFileAsync(rootPath, uploadedFile, folderName);

            if (!success || file == null)
                return $"فشل رفع الملف: {error ?? "Couldn't retrieve file info"}";

            // Queue for DB save
            await db.Files.AddAsync(file, ct);
            entity.FileId = file.Id;

            // Register cleanup event
            errorEvents.Add(async () =>
            {
                var path = Path.Combine(file.FolderPath, $"{file.Id}{file.Extension}");
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning("Cleaning up failed upload: {FullPath}", Path.Combine(rootPath, path));
                await CleanupFile(rootPath, logger, path);
            });

            return null;
        }

        public static async Task<string?> TryUploadAsync<TEntity>(
            string rootPath,
            IErrorEventCollector errorEvents,
            ILogger logger,
            AppData db,
            TEntity entity,
            IFormFile? uploadedFile,
            string folderName,
            CancellationToken ct)
            where TEntity : BaseEntity, IHasFile
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
                return "No file uploaded";

            var (success, file, error) = await SaveFileAsync(rootPath, uploadedFile, folderName);

            if (!success || file == null)
                return $"فشل رفع الملف: {error ?? "Couldn't retrieve file info"}";

            // Queue for DB save
            await db.Files.AddAsync(file, ct);
            entity.FileId = file.Id;

            // Register cleanup event
            errorEvents.Add(async () =>
            {
                var path = Path.Combine(file.FolderPath, $"{file.Id}{file.Extension}");
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning("Cleaning up failed upload: {FullPath}", Path.Combine(rootPath, path));
                await CleanupFile(rootPath, logger, path);
            });

            return null;
        }
    }
}
