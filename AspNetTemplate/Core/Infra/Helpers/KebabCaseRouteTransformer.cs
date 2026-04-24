using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace AspNetTemplate.Core.Infra.Helpers
{
    public sealed class KebabCaseRouteTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value) =>
            value is null ? null
            : Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1-$2").ToLower();
    }
}
