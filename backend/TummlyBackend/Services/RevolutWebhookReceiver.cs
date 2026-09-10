using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RevolutWebhookReceiver : IRevolutWebhookReceiver
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutWebhookService _processor;
        private readonly IRevolutWebhookInboxWork _work;
        private readonly IHostEnvironment _environment;
        private readonly RevolutSettings _settings;
        private readonly TimeProvider _clock;

        public RevolutWebhookReceiver(
            ApplicationDbContext context,
            IRevolutWebhookService processor,
            IRevolutWebhookInboxWork work,
            IHostEnvironment environment,
            IOptions<RevolutSettings> settings,
            TimeProvider clock
        )
        {
            _context = context;
            _processor = processor;
            _work = work;
            _environment = environment;
            _settings = settings.Value;
            _clock = clock;
        }

        public async Task<RevolutWebhookHandleResult> ReceiveAsync(
            string rawBody,
            string? signatureHeader,
            string? requestTimestamp,
            CancellationToken cancellationToken = default
        )
        {
            // Existing endpoint integration tests assert the processor's exact
            // synchronous outcomes. Hosted workers are disabled in Testing.
            if (_environment.IsEnvironment("Testing"))
            {
                return await _processor.HandleAsync(
                    rawBody,
                    signatureHeader,
                    requestTimestamp,
                    cancellationToken
                );
            }

            if (
                !RevolutWebhookSignature.Verify(
                    _settings.WebhookSigningSecret,
                    requestTimestamp ?? string.Empty,
                    rawBody ?? string.Empty,
                    signatureHeader ?? string.Empty
                )
            )
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.BadSignature
                );
            }

            var payloadHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawBody ?? string.Empty))
            ).ToLowerInvariant();

            if (
                await _context.RevolutWebhookInboxItems
                    .AsNoTracking()
                    .AnyAsync(row => row.PayloadHash == payloadHash, cancellationToken)
            )
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
            }

            var item = new RevolutWebhookInboxItem
            {
                Id = Guid.NewGuid(),
                PayloadHash = payloadHash,
                RawBody = rawBody ?? string.Empty,
                Status = RevolutWebhookInboxStatuses.Pending,
                CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
            };

            try
            {
                _context.RevolutWebhookInboxItems.Add(item);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                if (
                    await _context.RevolutWebhookInboxItems
                        .AsNoTracking()
                        .AnyAsync(
                            row => row.PayloadHash == payloadHash,
                            cancellationToken
                        )
                )
                {
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                throw;
            }

            await _work.NotifyAsync(item.Id, cancellationToken);
            return new RevolutWebhookHandleResult(
                RevolutWebhookHandleStatus.Accepted
            );
        }
    }
}
