using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/campaign-templates")]
    [Authorize]
    public class CampaignTemplatesController : ControllerBase
    {
        private readonly ICampaignTemplateCatalogueService _catalogue;

        public CampaignTemplatesController(
            ICampaignTemplateCatalogueService catalogue
        )
        {
            _catalogue = catalogue;
        }

        /*
         =========================================
         PRODUCT-GLOBAL TEMPLATE CATALOGUE (READ-ONLY)
         =========================================
        */

        [HttpGet]
        public IActionResult ListTemplates()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            try
            {
                var response = _catalogue.List();
                return Ok(new
                {
                    success = true,
                    items = response.Items,
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetTemplate(string id)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var template = _catalogue.GetById(id);
            if (template == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Campaign template not found.",
                });
            }

            return Ok(new
            {
                success = true,
                template,
            });
        }
    }
}
