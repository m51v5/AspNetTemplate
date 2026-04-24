using AspNetTemplate.Core.Data.DbContexts;
using Laraue.EfCoreTriggers.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection;

namespace AspNetTemplate.Core.Data.Base;

/// <summary>
/// Base configuration for any entity implementing IHasOptionalFile.
/// Registers DB triggers that keep UploadedFile.UsageCount accurate on INSERT / UPDATE / DELETE.
/// Inherit this in the entity's IEntityTypeConfiguration and call base.Configure(builder).
/// </summary>
public abstract class OptionalFileUsageTriggerConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity, IHasOptionalFile
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // INSERT — increment when a file is attached on row creation
        builder.AfterInsert(trigger => trigger
            .Action(action => action
                .Condition(refs => refs.New.FileId != null)
                .Update<UploadedFile>(
                    (refs, file) => refs.New.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount + 1 })));

        // DELETE — decrement when the entity is deleted
        builder.AfterDelete(trigger => trigger
            .Action(action => action
                .Condition(refs => refs.Old.FileId != null)
                .Update<UploadedFile>(
                    (refs, file) => refs.Old.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount - 1 })));

        // UPDATE — swap: decrement old file, increment new file (only when FileId actually changes)
        builder.AfterUpdate(trigger => trigger
            .Action(action => action
                .Condition(refs => refs.Old.FileId != null && refs.Old.FileId != refs.New.FileId)
                .Update<UploadedFile>(
                    (refs, file) => refs.Old.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount - 1 }))
            .Action(action => action
                .Condition(refs => refs.New.FileId != null && refs.Old.FileId != refs.New.FileId)
                .Update<UploadedFile>(
                    (refs, file) => refs.New.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount + 1 })));
    }
}

/// <summary>
/// Base configuration for any entity implementing IHasFile (non-nullable FileId).
/// Same trigger semantics as FileUsageTriggerConfiguration but without null guards on INSERT/DELETE.
/// </summary>
public abstract class FileUsageTriggerConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity, IHasFile
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // INSERT — FileId is always set, always increment
        builder.AfterInsert(trigger => trigger
            .Action(action => action
                .Update<UploadedFile>(
                    (refs, file) => refs.New.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount + 1 })));

        // DELETE — always decrement
        builder.AfterDelete(trigger => trigger
            .Action(action => action
                .Update<UploadedFile>(
                    (refs, file) => refs.Old.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount - 1 })));

        // UPDATE — swap only when FileId actually changes
        builder.AfterUpdate(trigger => trigger
            .Action(action => action
                .Condition(refs => refs.Old.FileId != refs.New.FileId)
                .Update<UploadedFile>(
                    (refs, file) => refs.Old.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount - 1 }))
            .Action(action => action
                .Condition(refs => refs.Old.FileId != refs.New.FileId)
                .Update<UploadedFile>(
                    (refs, file) => refs.New.FileId == file.Id,
                    (refs, file) => new UploadedFile { UsageCount = file.UsageCount + 1 })));
    }
}

/// <summary>
/// Startup guard — throws on app start if any entity implementing IHasOptionalFile
/// does not have a configuration class inheriting FileUsageTriggerConfiguration&lt;T&gt;.
/// Call after ApplyConfigurationsFromAssembly in OnModelCreating.
/// </summary>
public static class FileUsageTriggerGuard
{
    public static void Validate(ModelBuilder modelBuilder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var requiredEntities = modelBuilder.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .Where(t => typeof(IHasOptionalFile).IsAssignableFrom(t)
                     || typeof(IHasFile).IsAssignableFrom(t))
            .ToList();

        var coveredEntities = assembly.GetTypes()
            .Where(t => t.BaseType?.IsGenericType == true
                     && (t.BaseType.GetGenericTypeDefinition() == typeof(OptionalFileUsageTriggerConfiguration<>)
                      || t.BaseType.GetGenericTypeDefinition() == typeof(FileUsageTriggerConfiguration<>)))
            .Select(t => t.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        var missing = requiredEntities.Where(e => !coveredEntities.Contains(e)).ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"The following entities implement IHasOptionalFile but their configuration does not " +
                $"inherit FileUsageTriggerConfiguration<T>: " +
                $"{string.Join(", ", missing.Select(t => t.Name))}. " +
                $"File usage count triggers will not fire for these entities.");
    }
}
