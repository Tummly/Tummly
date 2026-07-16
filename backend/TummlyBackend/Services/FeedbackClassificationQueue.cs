using System.Threading.Channels;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class FeedbackClassificationQueue
        : IFeedbackClassificationQueue
    {
        private readonly Channel<int> _channel =
            Channel.CreateUnbounded<int>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                }
            );

        public ChannelReader<int> Reader => _channel.Reader;

        public ValueTask EnqueueAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
            => _channel.Writer.WriteAsync(feedbackId, cancellationToken);
    }
}
