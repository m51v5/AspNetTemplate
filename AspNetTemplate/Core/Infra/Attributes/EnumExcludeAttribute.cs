using System.ComponentModel.DataAnnotations;

namespace AspNetTemplate.Core.Infra.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class EnumExcludeAttribute(params object[] excluded) : ValidationAttribute
    {
        private readonly object[] _excluded = excluded;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null)
            {
                foreach (var excludedValue in _excluded)
                {
                    if (value.Equals(excludedValue))
                    {
                        return new ValidationResult(ErrorMessage ?? $"The value '{value}' is not allowed for {validationContext.DisplayName}.");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}
