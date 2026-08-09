using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin Campaign Draft create / get / PATCH — status always draft (ticket 29).
    /// </summary>
    public class CampaignDraftService : ICampaignDraftService
    {
        public const string DraftStatus = "draft";

        private static readonly IReadOnlyDictionary<string, string> GoalDefaultNames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["thank-recent-guests"] = "Thank recent guests",
                ["boost-quieter-time"] = "Boost a quieter time",
                ["re-engage-inactive"] = "Re-engage inactive guests",
                ["promote-something-new"] = "Promote something new",
                ["follow-up-completed-recovery"] = "Follow up after completed recovery",
                ["custom-campaign"] = "Custom campaign",
            };

        private readonly ApplicationDbContext _context;
        private readonly ICampaignTemplateCatalogueService _templates;
        private readonly IOffersCatalogService _offers;

        public CampaignDraftService(
            ApplicationDbContext context,
            ICampaignTemplateCatalogueService templates,
            IOffersCatalogService offers
        )
        {
            _context = context;
            _templates = templates;
            _offers = offers;
        }

        public async Task<CampaignDraftDto> CreateAsync(
            CreateCampaignDraftRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.LocationId < 1)
            {
                throw new ArgumentException("locationId is required.");
            }

            var goalId = NormalizeOptional(request.GoalId);
            var audienceKey = NormalizeOptional(request.AudienceKey);
            var channel = NormalizeOptional(request.Channel);
            var offerStance = NormalizeOptional(request.OfferStance);
            var templateId = NormalizeOptional(request.TemplateId);
            var templateVersion = ResolveTemplateSnapshot(
                templateId,
                request.TemplateVersion
            );

            CampaignProductAllowLists.EnsureOptionalGoalId(goalId);
            CampaignProductAllowLists.EnsureOptionalAudienceKey(audienceKey);
            CampaignProductAllowLists.EnsureOptionalChannel(channel);
            CampaignProductAllowLists.EnsureOptionalOfferStance(offerStance);

            var offerId = await ResolveOfferIdAsync(
                request.LocationId,
                offerStance,
                request.OfferId,
                cancellationToken
            );

            var name = ResolveName(request, templateId, goalId);
            var now = DateTime.UtcNow;

            var entity = new Campaign
            {
                RestaurantLocationId = request.LocationId,
                Status = DraftStatus,
                Name = name,
                GoalId = goalId,
                TemplateId = templateId,
                TemplateVersion = templateVersion,
                AudienceKey = audienceKey,
                Channel = channel,
                OfferStance = offerStance,
                OfferId = offerId,
                MessageSubject = NormalizeOptional(request.MessageSubject),
                MessageBody = NormalizeOptional(request.MessageBody),
                CreatedAt = now,
                UpdatedAt = now,
            };

            _context.Campaigns.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(entity);
        }

        public async Task<CampaignDraftDto?> GetByIdAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.Campaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    campaign =>
                        campaign.Id == campaignId
                        && campaign.Status == DraftStatus,
                    cancellationToken
                );

            return entity == null ? null : ToDto(entity);
        }

        public async Task<int?> GetLocationIdAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.Id == campaignId)
                .Select(campaign => (int?)campaign.RestaurantLocationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<CampaignDraftWriteResult> PatchAsync(
            int campaignId,
            PatchCampaignDraftRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.Campaigns
                .FirstOrDefaultAsync(
                    campaign => campaign.Id == campaignId,
                    cancellationToken
                );

            if (entity == null)
            {
                return new CampaignDraftWriteResult.NotFound();
            }

            if (!string.Equals(entity.Status, DraftStatus, StringComparison.Ordinal))
            {
                return new CampaignDraftWriteResult.NotDraft();
            }

            if (
                request.RowVersion.Length == 0
                || !entity.RowVersion.AsSpan().SequenceEqual(request.RowVersion)
            )
            {
                return new CampaignDraftWriteResult.Conflict();
            }

            ApplyPatch(entity, request);
            await ApplyOfferAttachAsync(entity, request, cancellationToken);
            entity.Status = DraftStatus;
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new CampaignDraftWriteResult.Conflict();
            }

            return new CampaignDraftWriteResult.Ok { Campaign = ToDto(entity) };
        }

        private string ResolveName(
            CreateCampaignDraftRequest request,
            string? templateId,
            string? goalId
        )
        {
            var explicitName = NormalizeOptional(request.Name);
            if (explicitName != null)
            {
                return explicitName;
            }

            if (templateId != null)
            {
                var template = _templates.GetById(templateId);
                if (template != null && !string.IsNullOrWhiteSpace(template.Title))
                {
                    return template.Title.Trim();
                }
            }

            if (
                goalId != null
                && GoalDefaultNames.TryGetValue(goalId, out var goalName)
            )
            {
                return goalName;
            }

            throw new ArgumentException(
                "name is required when no template title or goal label can supply a default."
            );
        }

        private void ApplyPatch(
            Campaign entity,
            PatchCampaignDraftRequest request
        )
        {
            if (request.Name != null)
            {
                var name = NormalizeOptional(request.Name);
                if (name == null)
                {
                    throw new ArgumentException("name cannot be empty.");
                }

                entity.Name = name;
            }

            if (request.GoalId != null)
            {
                var goalId = NormalizeOptional(request.GoalId);
                CampaignProductAllowLists.EnsureOptionalGoalId(goalId);
                entity.GoalId = goalId;
            }

            if (request.AudienceKey != null)
            {
                var audienceKey = NormalizeOptional(request.AudienceKey);
                CampaignProductAllowLists.EnsureOptionalAudienceKey(audienceKey);
                entity.AudienceKey = audienceKey;
            }

            if (request.Channel != null)
            {
                var channel = NormalizeOptional(request.Channel);
                CampaignProductAllowLists.EnsureOptionalChannel(channel);
                entity.Channel = channel;
            }

            if (request.OfferStance != null)
            {
                var offerStance = NormalizeOptional(request.OfferStance);
                CampaignProductAllowLists.EnsureOptionalOfferStance(offerStance);
                entity.OfferStance = offerStance;
            }

            if (request.TemplateId != null || request.TemplateVersion.HasValue)
            {
                var templateId = request.TemplateId != null
                    ? NormalizeOptional(request.TemplateId)
                    : entity.TemplateId;

                if (request.TemplateId != null && templateId == null)
                {
                    entity.TemplateId = null;
                    entity.TemplateVersion = null;
                }
                else
                {
                    // New templateId without version → stamp catalogue version.
                    // Version-only patch → validate against the current template id.
                    int? requestedVersion = request.TemplateVersion.HasValue
                        ? request.TemplateVersion
                        : request.TemplateId != null
                            ? null
                            : entity.TemplateVersion;

                    entity.TemplateId = templateId;
                    entity.TemplateVersion = ResolveTemplateSnapshot(
                        templateId,
                        requestedVersion
                    );
                }
            }

            if (entity.TemplateId == null)
            {
                entity.TemplateVersion = null;
            }

            if (request.MessageSubject != null)
            {
                entity.MessageSubject = NormalizeOptional(request.MessageSubject);
            }

            if (request.MessageBody != null)
            {
                entity.MessageBody = NormalizeOptional(request.MessageBody);
            }
        }

        private async Task ApplyOfferAttachAsync(
            Campaign entity,
            PatchCampaignDraftRequest request,
            CancellationToken cancellationToken
        )
        {
            // No offer always clears the attach.
            if (string.Equals(entity.OfferStance, "no-offer", StringComparison.Ordinal))
            {
                entity.OfferId = null;
                return;
            }

            if (!request.OfferId.HasValue)
            {
                return;
            }

            var offerId = request.OfferId.Value;
            if (offerId < 1)
            {
                throw new ArgumentException("offerId is invalid.");
            }

            var ok = await _offers.IsActiveForLocationAsync(
                offerId,
                entity.RestaurantLocationId,
                cancellationToken
            );
            if (!ok)
            {
                throw new ArgumentException(
                    "offerId must reference an Active Offers catalog definition for this location."
                );
            }

            entity.OfferId = offerId;
        }

        private async Task<int?> ResolveOfferIdAsync(
            int locationId,
            string? offerStance,
            int? requestedOfferId,
            CancellationToken cancellationToken
        )
        {
            if (string.Equals(offerStance, "no-offer", StringComparison.Ordinal))
            {
                return null;
            }

            if (!requestedOfferId.HasValue)
            {
                return null;
            }

            var offerId = requestedOfferId.Value;
            if (offerId < 1)
            {
                throw new ArgumentException("offerId is invalid.");
            }

            var ok = await _offers.IsActiveForLocationAsync(
                offerId,
                locationId,
                cancellationToken
            );
            if (!ok)
            {
                throw new ArgumentException(
                    "offerId must reference an Active Offers catalog definition for this location."
                );
            }

            return offerId;
        }

        private int? ResolveTemplateSnapshot(
            string? templateId,
            int? templateVersion
        )
        {
            if (templateId == null)
            {
                return null;
            }

            var template = _templates.GetById(templateId);
            if (template == null)
            {
                throw new ArgumentException(
                    $"templateId '{templateId}' is not in the campaign template catalogue."
                );
            }

            if (templateVersion.HasValue && templateVersion.Value != template.Version)
            {
                throw new ArgumentException(
                    $"templateVersion '{templateVersion.Value}' does not match the catalogue template."
                );
            }

            return template.Version;
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static CampaignDraftDto ToDto(Campaign entity)
        {
            return new CampaignDraftDto
            {
                Id = entity.Id,
                LocationId = entity.RestaurantLocationId,
                Status = entity.Status,
                Name = entity.Name,
                GoalId = entity.GoalId,
                TemplateId = entity.TemplateId,
                TemplateVersion = entity.TemplateVersion,
                AudienceKey = entity.AudienceKey,
                Channel = entity.Channel,
                OfferStance = entity.OfferStance,
                OfferId = entity.OfferId,
                MessageSubject = entity.MessageSubject,
                MessageBody = entity.MessageBody,
                RowVersion = entity.RowVersion,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }
    }
}
