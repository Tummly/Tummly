using System.Threading.Channels;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Bounded in-memory queue and worker for guest preference centre sends. Each send
    /// opens its own DI scope, so the scoped email and SMS services are resolved there.
    /// A send that fails is logged with the masked contact and dropped. The guest already
    /// got the same 202 response.
    /// </summary>
    public sealed class GuestPreferencesSendWorker : IGuestPreferencesSendQueue
    {
        public const int Capacity = 1000;

        private readonly Channel<GuestPreferencesSendItem> _channel =
            Channel.CreateBounded<GuestPreferencesSendItem>(
                new BoundedChannelOptions(Capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleWriter = false,
                    SingleReader = false,
                }
            );

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GuestPreferencesSendWorker> _logger;

        public GuestPreferencesSendWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<GuestPreferencesSendWorker> logger
        )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool TryEnqueue(GuestPreferencesSendItem item)
        {
            if (_channel.Writer.TryWrite(item))
            {
                return true;
            }

            _logger.LogError(
                "Guest preferences send queue is full. Send dropped for {MaskedContact}",
                Mask(item)
            );
            return false;
        }

        /// <summary>Host loop. Runs until the host stops.</summary>
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (
                    var item in _channel.Reader.ReadAllAsync(stoppingToken)
                )
                {
                    await SendAsync(item, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The host controls the shutdown window.
            }
        }

        /// <summary>
        /// Sends every item already in the queue, then returns. Lets tests run the worker
        /// without the host loop.
        /// </summary>
        public async Task DrainPendingAsync(CancellationToken ct)
        {
            while (_channel.Reader.TryRead(out var item))
            {
                await SendAsync(item, ct);
            }
        }

        private async Task SendAsync(GuestPreferencesSendItem item, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                if (item.ContactType == TummlyGuestContactTypes.Email)
                {
                    if (item.ConfirmUrl is null)
                    {
                        return;
                    }

                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    if (item.IsAddContact)
                    {
                        await email.SendGuestPreferencesAddContactConfirmEmailAsync(
                            item.Destination,
                            item.ConfirmUrl
                        );
                        return;
                    }

                    await email.SendGuestPreferencesConfirmEmailAsync(
                        item.Destination,
                        item.ConfirmUrl
                    );
                    return;
                }

                var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();
                await sms.SendOtpSmsAsync(item.Destination);
            }
            // A cancellation that is not host shutdown (for example an HttpClient timeout) is a
            // failed send. Only a cancelled ct stops the loop.
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogProviderSendFailure(
                    ex,
                    "Guest preferences send failed for {MaskedContact}",
                    Mask(item)
                );
            }
        }

        private static string Mask(GuestPreferencesSendItem item) =>
            item.ContactType == TummlyGuestContactTypes.Email
                ? LogContactMask.Email(item.Destination)
                : LogContactMask.Phone(item.Destination);
    }
}
