using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantAiBillingService : IAssistantAiBilling
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ApplicationDbContext _context;
        private readonly ICreditLedger _ledger;
        private readonly ICreditBalanceSnapshot _snapshot;
        private readonly TimeProvider _clock;

        public AssistantAiBillingService(
            ApplicationDbContext context,
            ICreditLedger ledger,
            ICreditBalanceSnapshot snapshot,
            TimeProvider clock
        )
        {
            _context = context;
            _ledger = ledger;
            _snapshot = snapshot;
            _clock = clock;
        }

        public async Task<AssistantConversationDto?> TryGetCachedOutcomeAsync(
            int restaurantId,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return null;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var row = await _context.AssistantAiActionOutcomes
                .AsNoTracking()
                .Where(item =>
                    item.RestaurantId == restaurantId
                    && item.IdempotencyKey == idempotencyKey
                    && item.ExpiresAtUtc > now
                )
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<AssistantConversationDto>(
                row.ResponseJson,
                JsonOptions
            );
        }

        public async Task<int?> TryResolveRestaurantIdAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == locationId)
                .Select(location => (int?)location.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> GetAiRemainingAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var snapshot = await _snapshot.GetAccountAsync(restaurantId, cancellationToken);
            if (snapshot is null)
            {
                return 0;
            }

            return snapshot.Channels
                .FirstOrDefault(channel => channel.Channel == CreditChannels.Ai)
                ?.Remaining
                ?? 0;
        }

        public Task<CreditLedgerWriteResult> ConsumeCompletedAnswerAsync(
            int restaurantId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return _ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Ai,
                    Units = AssistantAiBillingRules.CompletedAnswerUnits,
                    LocationId = locationId,
                },
                cancellationToken
            );
        }

        public async Task StoreOutcomeAsync(
            int restaurantId,
            string idempotencyKey,
            AssistantConversationDto conversation,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var json = JsonSerializer.Serialize(conversation, JsonOptions);
            _context.AssistantAiActionOutcomes.Add(
                new AssistantAiActionOutcome
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    IdempotencyKey = idempotencyKey.Trim(),
                    ResponseJson = json,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(AssistantAiBillingRules.IdempotencyTtl),
                }
            );
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
