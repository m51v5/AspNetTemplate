using FluentValidation;
using FluentValidation.Validators;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AspNetTemplate.Core.Infra.Filters;

/// <summary>
/// Schema filter that reads FluentValidation rules and appends them as
/// human-readable text to each property description in Swagger.
/// </summary>
public class FluentValidationDescriptionFilter(IServiceProvider serviceProvider) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is not { Count: > 0 }) return;

        var validatorType = typeof(IValidator<>).MakeGenericType(context.Type);
        using var scope = serviceProvider.CreateScope();
        if (scope.ServiceProvider.GetService(validatorType) is not IValidator validator) return;

        var descriptor = validator.CreateDescriptor();

        foreach (var (key, propSchema) in schema.Properties)
        {
            // FluentValidation stores member names in PascalCase
            var memberName = char.ToUpper(key[0]) + key[1..];
            var rules = descriptor.GetRulesForMember(memberName);

            var lines = rules
                .SelectMany(r => r.Components)
                .Select(c => Describe(c.Validator))
                .OfType<string>()
                .Distinct()
                .ToList();

            if (lines.Count == 0) continue;

            var annotation = string.Join(" | ", lines);
            propSchema.Description = string.IsNullOrWhiteSpace(propSchema.Description)
                ? annotation
                : $"{propSchema.Description} | {annotation}";
        }
    }

    private static string? Describe(IPropertyValidator v) => v switch
    {
        INotEmptyValidator or INotNullValidator => "Required",
        IMaximumLengthValidator mv              => $"Max length: {mv.Max}",
        IMinimumLengthValidator mv              => $"Min length: {mv.Min}",
        ILengthValidator lv                     => $"Length: {lv.Min}–{lv.Max}",
        IBetweenValidator bv                    => $"Range: {bv.From}–{bv.To}",
        IComparisonValidator cv                 => $"{ComparisonSymbol(cv.Comparison)} {cv.ValueToCompare}",
        IEmailValidator                         => "Valid email",
        IRegularExpressionValidator rv          => $"Pattern: {rv.Expression}",
        _ when v.GetType().Name.StartsWith("IsInEnum") => "Must be a valid enum value",
        _ => null
    };

    private static string ComparisonSymbol(Comparison c) => c switch
    {
        Comparison.GreaterThan        => ">",
        Comparison.GreaterThanOrEqual => ">=",
        Comparison.LessThan           => "<",
        Comparison.LessThanOrEqual    => "<=",
        Comparison.Equal              => "=",
        Comparison.NotEqual           => "!=",
        _                             => "?"
    };
}
