namespace AspNetTemplate.Core.Infra.Helpers
{
    internal static class EnvReader
    {
        private const string EnvFileName = ".env";

        public static string? GetVariable(string keyName)
        {
            if (!File.Exists(EnvFileName))
                return null;

            using var reader = new StreamReader(EnvFileName);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!key.Equals(keyName, StringComparison.Ordinal))
                    continue;

                // Remove surrounding quotes if present
                if (
                    (value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\''))
                )
                {
                    value = value[1..^1];
                }

                return value;
            }

            return null;
        }
    }
}
