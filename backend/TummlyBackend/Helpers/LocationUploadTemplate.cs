namespace TummlyBackend.Helpers
{
    public static class LocationUploadTemplate
    {
        public const string FileName = "tummly-locations-template.csv";

        public const string ContentType = "text/csv";

        public static string GetTemplatePath(IWebHostEnvironment environment)
        {
            return Path.Combine(
                environment.ContentRootPath,
                "Assets",
                "Templates",
                FileName
            );
        }
    }
}
