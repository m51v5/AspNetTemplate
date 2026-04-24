using BCrypt.Net;
using System.Text.RegularExpressions;

namespace AspNetTemplate.Core.Infra.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Hashes the string as a password using BCrypt.
        /// </summary>
        public static string AsHashedPassword(this string password)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        }

        /// <summary>
        /// Check if not null nor empty
        /// </summary>
        public static bool IsUnValidString(this string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsValidString(this string? str)
        {
            return !str.IsUnValidString();
        }

        /// <summary>
        /// Verifies the string against a BCrypt hash.
        /// </summary>
        public static bool VerifyHashedPassword(this string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword);
        }

        /// <summary>
        ///     convert from "How you Doin" to "how-you-doin" (all lower, seprated by dash)
        /// </summary>
        public static string ToKebabCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // First: handle acronym boundaries (ABCd -> AB-Cd)
            input = Regex.Replace(
                input,
                "([A-Z]+)([A-Z][a-z])",
                "$1-$2"
            );

            // Second: handle normal camel/PascalCase (aB -> a-B)
            input = Regex.Replace(
                input,
                "([a-z0-9])([A-Z])",
                "$1-$2"
            );

            return input.ToLowerInvariant();
        }
    }
}
