using System.Reflection;
using AspNetTemplate.Core.Infra.Attributes;
using AspNetTemplate.Core.Infra.Extensions;
using AspNetTemplate.Core.Data.Common;
using AspNetTemplate.Data;
using Microsoft.OpenApi;

namespace AspNetTemplate.Core.Endpoints
{
    public static class EnumsEndpoints
    {
        public static void MapEnumEndpoints(this WebApplication app)
        {
            var enumTypes = typeof(Enums).GetNestedTypes()
                .Where(t => t.IsEnum && t.GetCustomAttribute<RegisterEnumAsEndpointAttribute>() != null);

            foreach (var enumType in enumTypes)
            {
                var route = $"/api/enums/{enumType.Name.ToKebabCase()}";

                app.MapGet(route, () =>
                    {
                        var values = Enum.GetValues(enumType)
                            .Cast<Enum>()
                            .Select(e => new EnumResponseDto
                            {
                                Id = Convert.ToInt32(e),
                                Name = e.ToString(),
                                Value = e.GetDisplayName()
                            });

                        return Results.Ok(new SuccessResponse<IEnumerable<EnumResponseDto>?>(values));
                    })
                    .WithName(enumType.Name)
                    .WithTags("Enums");
            }
        }
    }
}
