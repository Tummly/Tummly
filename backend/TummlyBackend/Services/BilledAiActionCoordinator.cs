using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class BilledAiActionCoordinator : IBilledAiActionCoordinator
    {
        private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

        private readonly ApplicationDbContext _context;
        private readonly ICreditBalanceSnapshot _snapshot;
        private readonly ICreditLedger _ledger;
        private readonly TimeProvider _clock;

        public BilledAiActionCoordinator(
            ApplicationDbContext context,
            ICreditBalanceSnapshot snapshot,
            ICreditLedger ledger,
            TimeProvider clock
        )
        {
            _context = context;
            _snapshot = snapshot;
            _ledger = ledger;
            _clock = clock;
        }

        public async Task<BilledAiActionResult> ExecuteAsync(
            BilledAiActionRequest request,
            Func<CancellationToken, Task<BilledAiGenerationResult>> generateAsync,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return new BilledAiActionResult.IdempotencyKeyRequired();
            }

            var idempotencyKey = request.IdempotencyKey.Trim();
            var now = _clock.GetUtcNow().UtcDateTime;

            var cached = await _context.AiActionIdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == request.RestaurantId
                        && row.IdempotencyKey == idempotencyKey
                        && row.ExpiresAtUtc > now,
                    cancellationToken
                );
            if (cached != null)
            {
                return new BilledAiActionResult.Cached(ToPayload(cached));
            }

            var remaining = await ReadAiRemainingAsync(
                request.RestaurantId,
                cancellationToken
            );
            if (remaining <= 0)
            {
                return new BilledAiActionResult.HardStopped(remaining);
            }

            var generated = await generateAsync(cancellationToken);
            if (generated is BilledAiGenerationResult.NotFound notFound)
            {
                return new BilledAiActionResult.ResourceNotFound(notFound.Message);
            }

            if (generated is BilledAiGenerationResult.Failed failed)
            {
                return new BilledAiActionResult.ProviderFailed(
                    failed.Message,
                    failed.Retryable
                );
            }

            var payload = ((BilledAiGenerationResult.Ok)generated).Payload;
            var consumeResult = await _ledger.ConsumeOnSuccessAsync(
                new CreditLedgerConsumeRequest
                {
                    RestaurantId = request.RestaurantId,
                    Channel = CreditChannels.Ai,
                    Units = 1,
                    LocationId = request.LocationId,
                },
                cancellationToken
            );
            if (!consumeResult.Succeeded)
            {
                var code = consumeResult.Code ?? "insufficient_credits";
                var remainingAfter = await ReadAiRemainingAsync(
                    request.RestaurantId,
                    cancellationToken
                );
                return new BilledAiActionResult.ConsumeFailed(
                    code,
                    remainingAfter
                );
            }

            _context.AiActionIdempotencyRecords.Add(
                new AiActionIdempotencyRecord
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    IdempotencyKey = idempotencyKey,
                    PackKey = request.PackKey,
                    Body = payload.Body,
                    Subject = payload.Subject,
                    Channel = payload.Channel,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(IdempotencyTtl),
                }
            );
            await _context.SaveChangesAsync(cancellationToken);

            return new BilledAiActionResult.Succeeded(payload);
        }

        private async Task<int> ReadAiRemainingAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var account = await _snapshot.GetAccountAsync(
                restaurantId,
                cancellationToken
            );
            if (account == null)
            {
                return 0;
            }

            var ai = account.Channels.FirstOrDefault(
                row => row.Channel == CreditChannels.Ai
            );
            return ai?.Remaining ?? 0;
        }

        private static BilledAiDraftPayload ToPayload(
            AiActionIdempotencyRecord cached
        )
        {
            return new BilledAiDraftPayload
            {
                Body = cached.Body,
                Subject = cached.Subject,
                Channel = cached.Channel,
            };
        }
    }
}
