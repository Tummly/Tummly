using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
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

        public CampaignDraftService(
            ApplicationDbContext context,
            ICampaignTemplateCatalogueService templates
        )
        {
            _context = context;
            _templates = templates;
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

            var name = ResolveName(request);
            var now = DateTime.UtcNow;

            var entity = new Campaign
            {
                RestaurantLocationId = request.LocationId,
                Status = DraftStatus,
                Name = name,
                GoalId = NormalizeOptional(request.GoalId),
                TemplateId = NormalizeOptional(request.TemplateId),
                TemplateVersion = request.TemplateVersion,
                AudienceKey = NormalizeOptional(request.AudienceKey),
                Channel = NormalizeOptional(request.Channel),
                OfferStance = NormalizeOptional(request.OfferStance),
                MessageSubject = NormalizeOptional(request.MessageSubject),
                MessageBody = NormalizeOptional(request.MessageBody),
                RowVersion = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };

            if (entity.TemplateId == null)
            {
                entity.TemplateVersion = null;
            }

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
                    campaign => campaign.Id == campaignId,
                    cancellationToken
                );

            return entity == null ? null : ToDto(entity);
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

            if (entity.RowVersion != request.RowVersion)
            {
                return new CampaignDraftWriteResult.Conflict();
            }

            if (!string.Equals(entity.Status, DraftStatus, StringComparison.Ordinal))
            {
                return new CampaignDraftWriteResult.Conflict();
            }

            ApplyPatch(entity, request);
            entity.Status = DraftStatus;
            entity.RowVersion += 1;
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

        private string ResolveName(CreateCampaignDraftRequest request)
        {
            var explicitName = NormalizeOptional(request.Name);
            if (explicitName != null)
            {
                return explicitName;
            }

            var templateId = NormalizeOptional(request.TemplateId);
            if (templateId != null)
            {
                var template = _templates.GetById(templateId);
                if (template != null && !string.IsNullOrWhiteSpace(template.Title))
                {
                    return template.Title.Trim();
                }
            }

            var goalId = NormalizeOptional(request.GoalId);
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

        private static void ApplyPatch(
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
                entity.GoalId = NormalizeOptional(request.GoalId);
            }

            if (request.TemplateId != null)
            {
                entity.TemplateId = NormalizeOptional(request.TemplateId);
            }

            if (request.TemplateVersion.HasValue)
            {
                entity.TemplateVersion = request.TemplateVersion;
            }

            if (entity.TemplateId == null)
            {
                entity.TemplateVersion = null;
            }

            if (request.AudienceKey != null)
            {
                entity.AudienceKey = NormalizeOptional(request.AudienceKey);
            }

            if (request.Channel != null)
            {
                entity.Channel = NormalizeOptional(request.Channel);
            }

            if (request.OfferStance != null)
            {
                entity.OfferStance = NormalizeOptional(request.OfferStance);
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
                MessageSubject = entity.MessageSubject,
                MessageBody = entity.MessageBody,
                RowVersion = entity.RowVersion,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }
    }
}
