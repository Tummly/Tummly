using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class CampaignOutboundChannelSenderTests
    {
        [Fact]
        public async Task SendAsync_Sms_Accepted_MapsTwilioSegmentsToAcceptedUnits()
        {
            var sms = new RecordingSmsDelivery
            {
                Next = new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = 3,
                },
            };
            var sender = new CampaignOutboundChannelSender(
                email: new RejectingEmailOutbound(),
                smsDelivery: sms
            );

            var result = await sender.SendAsync(
                new CampaignOutboundSendRequest
                {
                    CampaignId = 1,
                    LocationGuestId = 2,
                    Channel = "sms",
                    ToAddress = "+447700900123",
                    Subject = null,
                    Body = "Thanks for visiting.",
                }
            );

            var accepted = Assert.IsType<CampaignOutboundSendResult.Accepted>(result);
            Assert.Equal(3, accepted.AcceptedUnits);
            Assert.Single(sms.Calls);
            Assert.Equal("+447700900123", sms.Calls[0].Phone);
            Assert.Equal("Thanks for visiting.", sms.Calls[0].Body);
        }

        [Fact]
        public async Task SendAsync_Sms_FailedDelivery_ReturnsRejected()
        {
            var sms = new RecordingSmsDelivery
            {
                Next = new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                },
            };
            var sender = new CampaignOutboundChannelSender(
                email: new RejectingEmailOutbound(),
                smsDelivery: sms
            );

            var result = await sender.SendAsync(
                new CampaignOutboundSendRequest
                {
                    CampaignId = 1,
                    LocationGuestId = 2,
                    Channel = "sms",
                    ToAddress = "+447700900123",
                    Subject = null,
                    Body = "Hi",
                }
            );

            var rejected = Assert.IsType<CampaignOutboundSendResult.Rejected>(result);
            Assert.Equal("Unable to send Recovery SMS.", rejected.Message);
        }

        [Fact]
        public async Task SendAsync_Email_DelegatesToEmailOutbound()
        {
            var email = new RecordingEmailOutbound
            {
                Next = new CampaignOutboundSendResult.Accepted { AcceptedUnits = 1 },
            };
            var sms = new RecordingSmsDelivery();
            var sender = new CampaignOutboundChannelSender(
                email: email,
                smsDelivery: sms
            );

            var result = await sender.SendAsync(
                new CampaignOutboundSendRequest
                {
                    CampaignId = 1,
                    LocationGuestId = 2,
                    Channel = "email",
                    ToAddress = "guest@example.com",
                    Subject = "Hello",
                    Body = "Body",
                }
            );

            Assert.IsType<CampaignOutboundSendResult.Accepted>(result);
            Assert.Single(email.Calls);
            Assert.Empty(sms.Calls);
        }

        private sealed class RecordingSmsDelivery : IRecoveryGuestSmsDelivery
        {
            public List<(string Phone, string Body)> Calls { get; } = [];

            public RecoveryGuestSmsDeliveryResult Next { get; set; } =
                new RecoveryGuestSmsDeliveryResult.Accepted { AcceptedSegments = 1 };

            public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
                string phoneNumber,
                string body,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((phoneNumber, body));
                return Task.FromResult(Next);
            }
        }

        private sealed class RejectingEmailOutbound : ICampaignOutboundSender
        {
            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<CampaignOutboundSendResult>(
                    new CampaignOutboundSendResult.Rejected
                    {
                        Message = "email path should not run",
                    }
                );
            }
        }

        private sealed class RecordingEmailOutbound : ICampaignOutboundSender
        {
            public List<CampaignOutboundSendRequest> Calls { get; } = [];

            public CampaignOutboundSendResult Next { get; set; } =
                new CampaignOutboundSendResult.Accepted();

            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request);
                return Task.FromResult(Next);
            }
        }
    }
}
