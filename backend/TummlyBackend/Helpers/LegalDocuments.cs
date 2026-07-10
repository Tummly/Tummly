namespace TummlyBackend.Helpers
{
    public static class LegalDocuments
    {
        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        public static readonly IReadOnlyDictionary<string, DocumentInfo> ByKey =
            new Dictionary<string, DocumentInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["privacy"] = new(
                    "Tummly_Privacy_Policy.docx",
                    "Tummly_Privacy_Policy.docx"
                ),
                ["terms"] = new(
                    "Tummly_Terms_and_Conditions.docx",
                    "Tummly_Terms_and_Conditions.docx"
                ),
                ["cookie-policy"] = new(
                    "Tummly_Cookie_Policy.docx",
                    "Tummly_Cookie_Policy.docx"
                ),
            };

        public static string GetDocumentPath(
            IWebHostEnvironment environment,
            string fileName
        )
        {
            return Path.Combine(
                environment.ContentRootPath,
                "Assets",
                "legal-docs",
                fileName
            );
        }

        public sealed record DocumentInfo(string FileName, string DownloadFileName);
    }
}
