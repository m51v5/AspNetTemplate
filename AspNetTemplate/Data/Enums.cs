using System.ComponentModel.DataAnnotations;
using AspNetTemplate.Core.Infra.Attributes;

namespace AspNetTemplate.Data
{
    public static class Enums
    {
        [RegisterEnumAsEndpoint]
        public enum AppRoles
        {
            None = 0,
            Admin,
            User,
        }
    }
}
