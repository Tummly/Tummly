using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/legal")]
    public class LegalController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public LegalController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("documents/{documentKey}")]
        public IActionResult DownloadLegalDocument(string documentKey)
        {
            if (!LegalDocuments.ByKey.TryGetValue(documentKey, out var document))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Legal document is unavailable.",
                });
            }

            var path = LegalDocuments.GetDocumentPath(
                _environment,
                document.FileName
            );

            if (!System.IO.File.Exists(path))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Legal document is unavailable.",
                });
            }

            return PhysicalFile(
                path,
                LegalDocuments.ContentType,
                document.DownloadFileName
            );
        }
    }
}
