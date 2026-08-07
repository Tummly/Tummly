using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignTemplateCatalogueService
    {
        /// <summary>
        /// Returns the launch catalogue. Empty catalogue is an error.
        /// </summary>
        CampaignTemplateListResponse List();

        /// <summary>
        /// Returns one template by stable slug id, or null when missing.
        /// </summary>
        CampaignTemplateDetailDto? GetById(string id);
    }
}
