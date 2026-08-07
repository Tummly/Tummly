using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Read-only campaign-template catalogue — seeded constants, no DB (ticket 21).
    /// </summary>
    public class CampaignTemplateCatalogueService : ICampaignTemplateCatalogueService
    {
        private readonly IReadOnlyList<CampaignTemplateDetailDto> _templates;

        public CampaignTemplateCatalogueService()
            : this(CampaignTemplateSeed.All)
        {
        }

        /// <summary>Test seam — inject an empty or alternate catalogue.</summary>
        public CampaignTemplateCatalogueService(
            IReadOnlyList<CampaignTemplateDetailDto> templates
        )
        {
            _templates = templates;
        }

        public CampaignTemplateListResponse List()
        {
            if (_templates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Campaign template catalogue is empty."
                );
            }

            return new CampaignTemplateListResponse
            {
                Items = _templates.Select(ToListItem).ToArray(),
            };
        }

        public CampaignTemplateDetailDto? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var key = id.Trim();
            return _templates.FirstOrDefault(template =>
                string.Equals(template.Id, key, StringComparison.Ordinal)
            );
        }

        private static CampaignTemplateListItemDto ToListItem(
            CampaignTemplateDetailDto detail
        )
        {
            return new CampaignTemplateListItemDto
            {
                Id = detail.Id,
                Version = detail.Version,
                Title = detail.Title,
                Description = detail.Description,
                GoalLabel = detail.GoalLabel,
                AudienceLabel = detail.AudienceLabel,
                ChannelLabel = detail.ChannelLabel,
                OfferLabel = detail.OfferLabel,
                SuggestsGoal = detail.SuggestsGoal,
                SuggestsAudience = detail.SuggestsAudience,
                SuggestsChannel = detail.SuggestsChannel,
                SuggestsOffer = detail.SuggestsOffer,
            };
        }
    }
}
