using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Stateless Campaign message-draft orchestration — no Draft persist, no cache,
    /// no credit ledger write (ticket 33 / contract 16).
    /// </summary>
    public sealed class CampaignMessageDraftService : ICampaignMessageDraftService
    {
        private readonly ICampaignMessageDraftProvider _provider;

        public CampaignMessageDraftService(ICampaignMessageDraftProvider provider)
        {
            _provider = provider;
        }

        public async Task<CampaignMessageDraftServiceResult> PrepareAsync(
            string locationName,
            PrepareCampaignMessageDraftRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var channel = request.Channel.Trim().ToLowerInvariant();
            if (channel is not ("email" or "sms"))
            {
                throw new ArgumentException("Channel must be email or sms.");
            }

            var mode = request.Mode?.Trim().ToLowerInvariant();
            if (mode is not (
                "prepare"
                or "rewrite_subject"
                or "rewrite_message"
            ))
            {
                throw new ArgumentException(
                    "Mode must be prepare, rewrite_subject, or rewrite_message."
                );
            }

            if (string.IsNullOrWhiteSpace(request.GoalId))
            {
                throw new ArgumentException("GoalId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.AudienceKey))
            {
                throw new ArgumentException("AudienceKey is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OfferStance))
            {
                throw new ArgumentException("OfferStance is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Tone))
            {
                throw new ArgumentException("Tone is required.");
            }

            var notes = string.IsNullOrWhiteSpace(request.IncludeNotes)
                ? null
                : request.IncludeNotes.Trim();
            var campaignName = string.IsNullOrWhiteSpace(request.CampaignName)
                ? null
                : request.CampaignName.Trim();

            var input = new CampaignMessageDraftInput(
                LocationName: locationName,
                Channel: channel,
                GoalId: request.GoalId.Trim(),
                AudienceKey: request.AudienceKey.Trim(),
                OfferStance: request.OfferStance.Trim(),
                CampaignName: campaignName,
                Tone: request.Tone.Trim(),
                IncludeNotes: notes,
                Mode: mode,
                CurrentBody: request.CurrentBody,
                CurrentSubject: request.CurrentSubject
            );

            CampaignMessageDraftProviderResult providerResult;
            try
            {
                providerResult = await _provider.DraftAsync(
                    input,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new CampaignMessageDraftServiceResult.Failed(
                    "We could not prepare a draft.",
                    Retryable: true
                );
            }

            return providerResult switch
            {
                CampaignMessageDraftProviderResult.Succeeded succeeded =>
                    new CampaignMessageDraftServiceResult.Ok(
                        succeeded.Body,
                        succeeded.Subject,
                        succeeded.Channel
                    ),
                CampaignMessageDraftProviderResult.Failed failed =>
                    new CampaignMessageDraftServiceResult.Failed(
                        "We could not prepare a draft.",
                        failed.Retryable
                    ),
                _ => new CampaignMessageDraftServiceResult.Failed(
                    "We could not prepare a draft.",
                    Retryable: true
                ),
            };
        }
    }
}
