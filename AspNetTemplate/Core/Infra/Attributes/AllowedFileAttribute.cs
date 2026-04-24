using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AspNetTemplate.Core.Infra.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowedFileAttribute(string[] allowedExtensions, long MaxSizeInMB, bool IsRequired = false) : ValidationAttribute
    {
        private readonly string[] allowedExtensions = allowedExtensions;
        private readonly long _maxMegaBytes = MaxSizeInMB * 1024 * 1024;
        private readonly bool _isRequired = IsRequired;

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var file = value as IFormFile;
            if (file == null)
            {
                return _isRequired
                        ? new ValidationResult($"{validationContext.MemberName} is required and can't be empty")
                        : ValidationResult.Success!;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (allowedExtensions.Length > 0 && !allowedExtensions.Contains(ext))
                return new ValidationResult($"File extension {ext} is not allowed.");

            if (file.Length > _maxMegaBytes)
                return new ValidationResult($"File size cannot exceed {_maxMegaBytes} MB.");

            return ValidationResult.Success!;
        }
    }

    public static class FileValidationExtensions
    {
        public static IRuleBuilderOptions<T, IFormFile?> AllowedFile<T>(
            this IRuleBuilder<T, IFormFile?> rule,
            string[]? allowedExtensions,
            long maxSizeInMB,
            bool isRequired = false)
        {
            long maxBytes = maxSizeInMB * 1024 * 1024;

            bool checkExtensions = allowedExtensions is { Length: > 0 };
            string[] normalizedExtensions = checkExtensions
                ? [.. allowedExtensions!.Select(e => e.ToLowerInvariant())]
                : [];

            // Required check
            if (isRequired)
            {
                rule = rule.NotNull().WithMessage("File is required.");
            }

            return (IRuleBuilderOptions<T, IFormFile?>)rule.Custom((file, context) =>
            {
                if (file == null)
                {
                    if (isRequired)
                        context.AddFailure("File is required.");
                    return;
                }

                // Extension check (only if provided)
                if (checkExtensions)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!normalizedExtensions.Contains(ext))
                    {
                        context.AddFailure($"File extension {ext} is not allowed.");
                        return;
                    }
                }

                // Max size check
                if (file.Length > maxBytes)
                {
                    context.AddFailure($"File size cannot exceed {maxSizeInMB} MB.");
                }
            });
        }
    }

}
