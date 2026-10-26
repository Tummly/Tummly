using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Records every Recovery SMS send. Returns the scripted results in order,
    /// then an accepted result with a callback (like the Testing host).
    /// </summary>
    public sealed class RecordingRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
    {
        private readonly Queue<RecoveryGuestSmsDeliveryResult> _scripted;
        private readonly object _gate = new();
        private readonly List<SentSms> _calls = new();

        public RecordingRecoveryGuestSmsDelivery(
            params RecoveryGuestSmsDeliveryResult[] scripted
        )
        {
            _scripted = new Queue<RecoveryGuestSmsDeliveryResult>(scripted);
        }

        public IReadOnlyList<SentSms> Calls
        {
            get
            {
                lock (_gate)
                {
                    return _calls.ToList();
                }
            }
        }

        public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            lock (_gate)
            {
                _calls.Add(new SentSms(phoneNumber, body));

                if (_scripted.Count > 0)
                {
                    return Task.FromResult(_scripted.Dequeue());
                }
            }

            return Task.FromResult<RecoveryGuestSmsDeliveryResult>(
                new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = CampaignSmsSegmentCalculator.CountSegments(body),
                    ProviderMessageId = "SMrecorded-" + Guid.NewGuid().ToString("N"),
                    StatusCallbackSet = true,
                }
            );
        }

        public sealed record SentSms(string PhoneNumber, string Body);
    }
}
