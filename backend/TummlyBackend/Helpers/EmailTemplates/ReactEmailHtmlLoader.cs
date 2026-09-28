namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Loads exported React Email HTML under Assets/emails/templates/.
    /// </summary>
    internal static class ReactEmailHtmlLoader
    {
        private static readonly Dictionary<string, string> Cache = new(
            StringComparer.Ordinal
        );

        public static string Load(
            IWebHostEnvironment environment,
            string templateRelativePath,
            string missingMessage
        )
        {
            if (Cache.TryGetValue(templateRelativePath, out var cached))
            {
                return cached;
            }

            var path = Path.Combine(
                environment.ContentRootPath,
                templateRelativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(missingMessage, path);
            }

            var html = File.ReadAllText(path);
            Cache[templateRelativePath] = html;
            return html;
        }
    }
}
