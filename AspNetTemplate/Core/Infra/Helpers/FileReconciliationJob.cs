using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetTemplate.Core.Infra.Helpers;

/// <summary>
/// Weekly Hangfire job that recomputes UploadedFile.UsageCount from scratch.
/// Corrects any drift caused by raw SQL migrations or bulk operations that bypassed DB triggers.
/// Does NOT delete files — deletion stays in CleanupOrphanUploadedFiles.
/// </summary>
public sealed class FileReconciliationJob(
    AppData context,
    ILogger<FileReconciliationJob> logger) : BaseService(context)
{
    private readonly ILogger<FileReconciliationJob> _logger = logger;

    // TODO : batch problem, if use batch it wont count correctly so it has to load all.
    //          need refactor and another idea for this.
    public async Task ReconcileAsync()
    {
        _logger.LogInformation("Starting file usage count reconciliation");

        // Collect all file IDs that are actually referenced across every IHasOptionalFile entity.
        // Add a new line here when a new entity with FileId is introduced.
        var referencedCounts = new Dictionary<Guid, int>();

        void Count(IEnumerable<Guid?> ids)
        {
            foreach (var id in ids)
            {
                if (id is null) continue;
                referencedCounts.TryGetValue(id.Value, out var current);
                referencedCounts[id.Value] = current + 1;
            }
        }

        void CountRequired(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                referencedCounts.TryGetValue(id, out var current);
                referencedCounts[id] = current + 1;
            }
        }

        // IMPORTANT: Add a Count(...) line for every entity that implements IHasOptionalFile / IHasFile.
        // The startup guard (FileUsageTriggerGuard) enforces trigger coverage but does NOT enforce
        // coverage here. A missing entity means drift in its file counts will never be corrected.
        Count(await _context.Users.Select(x => x.FileId).ToListAsync());

        var files = await _context.Files.ToListAsync();

        int corrected = 0;
        foreach (var file in files)
        {
            referencedCounts.TryGetValue(file.Id, out var expected);
            if (file.UsageCount != expected)
            {
                file.UsageCount = expected;
                corrected++;
            }
        }

        if (corrected > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogWarning("Reconciliation corrected {Count} file usage counts", corrected);
        }
        else
        {
            _logger.LogInformation("Reconciliation complete — all counts were accurate");
        }
    }
}
