using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.DbContexts;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Core.Infra.Helpers
{
    [AutoRegister]
    public sealed class CleanupOrphanUploadedFiles(
        AppData context,
        ILogger<CleanupOrphanUploadedFiles> logger) : BaseService(context)
    {
        static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<CleanupOrphanUploadedFiles> _logger = logger;

        public async Task CleanupOrphanedFilesAsync()
        {
            if (!_semaphore.Wait(0))
            {
                // Another cleanup is already in progress
                return;
            }

            try
            {
                await CleanInternal();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run orphan files cleanup");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CleanInternal()
        {
            _logger.LogInformation("Starting orphan files cleanup");

            const int batchSize = 200;

            DateTime cutoff = DateTime.UtcNow.AddDays(-5);

            var orphanFiles = await _context.Files
                .Where(f => f.UsageCount == 0 && f.CreatedAt < cutoff)
                .Take(batchSize)
                .ToArrayAsync();

            if (orphanFiles.Length == 0) return;


            foreach (UploadedFile file in orphanFiles)
            {
                var filePath = Path.Combine(file.FolderPath, $"{file.Id}{file.Extension}");

                await FileHelper.CleanupFile(file.StoragePath, _logger, filePath);
                _context.Files.Remove(file);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} orphan files", orphanFiles.Length);
        }
    }
}
