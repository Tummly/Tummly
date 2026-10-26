using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="TwilioSmsInboundService"/> — signature, STOP keyword, and
    /// rule A: STOP from E.164 withdraws SMS marketing at each restaurant that
    /// texted that phone and stops SMS there (R4.3).
    /// </summary>
    public class TwilioSmsInboundServiceTests : IDisposable
    {
        private const string AuthToken = "twilio_svc_auth_token";
        private const string SharedTo = "+447700900999";
        private const string GuestFrom = "+447700900111";
        private const string RequestUrl =
            "http://localhost/api/webhooks/twilio/sms-inbound";

        private readonly DateTime _now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly TwilioSmsInboundService _sut;

        public TwilioSmsInboundServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            var hasher = new GuestContactHasher(
                GuestContactHasher.TestingFallbackKey
            );
            var suppressions = new GuestContactSuppressionService(
                _context,
                hasher
            );
            var withdraw = new GuestInitiatedMarketingWithdrawService(
                _context,
                _permissions,
                suppressions,
                NullLogger<GuestInitiatedMarketingWithdrawService>.Instance
            );

            var settings = Options.Create(
                new TwilioSettings
                {
                    AuthToken = AuthToken,
                    DefaultRegion = "GB",
                }
            );

            _sut = new TwilioSmsInboundService(
                settings,
                _context,
                withdraw,
                NullLogger<TwilioSmsInboundService>.Instance
            );
        }

        [Fact]
        public async Task Stop_TextedByR1AndR2_WithdrawsAndStopsBoth_NotR3()
        {
            var r1 = await SeedGuestAsync("Venue One", GuestFrom, SmsTouch.FeedbackReply);
            var r2 = await SeedGuestAsync("Venue Two", GuestFrom, SmsTouch.CampaignSend);
            var r3 = await SeedGuestAsync("Venue Three", GuestFrom, SmsTouch.None);

            var status = await _sut.HandleAsync(
                RequestUrl,
                BuildForm(GuestFrom, SharedTo, "STOP"),
                Sign(BuildForm(GuestFrom, SharedTo, "STOP"))
            );

            Assert.Equal(TwilioSmsInboundStatus.Accepted, status);
            await AssertStoppedAsync(r1);
            await AssertStoppedAsync(r2);
            await AssertNotStoppedAsync(r3);
        }

        [Fact]
        public async Task Stop_CampaignSmsOnly_Counts()
        {
            // Accepted campaign SMS with no provider id still counts (controller ruling 2).
            var guest = await SeedGuestAsync("Campaign Venue", GuestFrom, SmsTouch.CampaignSend);
            var delivery = await _context.CampaignRecipientDeliveries.SingleAsync();
            delivery.ProviderMessageId = null;
            await _context.SaveChangesAsync();

            await HandleSignedStopAsync(GuestFrom);

            await AssertStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_CampaignSmsUnknownOutcome_Counts()
        {
            // The guest has no earlier SMS. The only SMS row comes from a real fire whose
            // provider call throws (the send may have reached the phone). The row is rejected.
            var guest = await SeedGuestAsync("Unknown Venue", GuestFrom, SmsTouch.None);
            await FireSmsCampaignToGuestAsync(
                guest,
                new ScriptedCampaignSmsDelivery(
                    () => throw new TimeoutException("The provider did not answer in time.")
                )
            );
            var row = await _context.CampaignRecipientDeliveries
                .AsNoTracking()
                .SingleAsync(d => d.LocationGuestId == guest.LocationGuestId);
            Assert.Equal(CampaignFireService.RejectedOutcome, row.Outcome);
            Assert.Null(row.AcceptedAtUtc);

            await HandleSignedStopAsync(GuestFrom);

            await AssertStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_CampaignSmsSkippedIneligible_DoesNotCount()
        {
            // A skipped-ineligible row means the recipient was never attempted.
            var guest = await SeedGuestAsync("Skipped Venue", GuestFrom, SmsTouch.CampaignSendSkippedIneligible);

            await HandleSignedStopAsync(GuestFrom);

            await AssertNotStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_UnknownPhone_NoChange()
        {
            var guest = await SeedGuestAsync("Venue", GuestFrom, SmsTouch.FeedbackReply);

            await HandleSignedStopAsync("+447700900222");

            await AssertNotStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_NonKeyword_NoChange()
        {
            var guest = await SeedGuestAsync("Venue", GuestFrom, SmsTouch.FeedbackReply);
            var form = BuildForm(GuestFrom, SharedTo, "HELLO");

            var status = await _sut.HandleAsync(RequestUrl, form, Sign(form));

            Assert.Equal(TwilioSmsInboundStatus.Accepted, status);
            await AssertNotStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_LegacyNationalFormatPhone_Matches()
        {
            // Pre-E.164 rows store the UK national form. STOP from +44 must still match.
            var guest = await SeedGuestAsync(
                "Legacy Venue",
                "07911123456",
                SmsTouch.FeedbackReply,
                normalizedPhone: "07911123456"
            );

            await HandleSignedStopAsync("+447911123456");

            await AssertStoppedAsync(guest);
        }

        [Fact]
        public async Task Stop_Twice_KeepsFirstStoppedAt()
        {
            var guest = await SeedGuestAsync("Venue", GuestFrom, SmsTouch.FeedbackReply);

            await HandleSignedStopAsync(GuestFrom);
            var first = await StoppedAtAsync(guest);
            await HandleSignedStopAsync(GuestFrom);

            Assert.Equal(first, await StoppedAtAsync(guest));
        }

        [Fact]
        public async Task Handle_BadSignature_ReturnsForbidden()
        {
            var guest = await SeedGuestAsync("Venue", GuestFrom, SmsTouch.FeedbackReply);
            var form = BuildForm(GuestFrom, SharedTo, "STOP");

            var status = await _sut.HandleAsync(RequestUrl, form, "bad-signature");

            Assert.Equal(TwilioSmsInboundStatus.Forbidden, status);
            await AssertNotStoppedAsync(guest);
        }

        /// <summary>
        /// Schedules one SMS campaign to the guest and runs the real fire path with the
        /// given provider double, through the real outbound channel sender.
        /// </summary>
        private async Task FireSmsCampaignToGuestAsync(
            SeededGuest guest,
            ScriptedCampaignSmsDelivery sms
        )
        {
            var locationId = await _context.LocationGuests
                .Where(lg => lg.Id == guest.LocationGuestId)
                .Select(lg => lg.RestaurantLocationId)
                .SingleAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Name = "STOP campaign",
                Status = CampaignFireService.ScheduledStatus,
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "sms",
                OfferStance = "no-offer",
                MessageBody = "Hello",
                ScheduleMode = "schedule-later",
                ScheduledAtUtc = _now.AddMinutes(-5),
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-stop-fire",
                ReservedEstimate = 10,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            _context.CampaignFrozenRecipients.Add(
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = guest.LocationGuestId,
                    FrozenAtUtc = _now.AddHours(-1),
                }
            );
            await _context.SaveChangesAsync();

            var outbound = new CampaignOutboundChannelSender(
                new UnusedCampaignEmailOutbound(),
                sms,
                new DemoSideEffectGuard(
                    Options.Create(
                        new DemoSessionOptions
                        {
                            Enabled = false,
                            SuppressSideEffects = false,
                        }
                    )
                )
                ,
                NullLogger<CampaignOutboundChannelSender>.Instance
            );
            var fire = new CampaignFireService(
                _context,
                new CampaignEligibilityService(_context),
                new PassiveCampaignBillingReserve(),
                outbound,
                new ClearCampaignSendStartGateDouble(),
                utcNow: () => _now
            );

            // One rejected recipient and no accepted send: the campaign closes as failed.
            // The test checks the delivery row that the fire wrote.
            await fire.FireAsync(campaign.Id);
        }

        private Task HandleSignedStopAsync(string from)
        {
            var form = BuildForm(from, SharedTo, "STOP");
            return _sut.HandleAsync(RequestUrl, form, Sign(form));
        }

        private static Dictionary<string, string> BuildForm(
            string from,
            string to,
            string body
        ) =>
            new()
            {
                ["From"] = from,
                ["To"] = to,
                ["Body"] = body,
            };

        private static string Sign(Dictionary<string, string> form) =>
            TwilioSmsInboundEndpointsTests.ComputeTwilioSignature(
                AuthToken,
                RequestUrl,
                form
            );

        private async Task AssertStoppedAsync(SeededGuest guest)
        {
            Assert.NotNull(await StoppedAtAsync(guest));
            var states = await _permissions.GetCurrentStatesAsync(guest.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        private async Task AssertNotStoppedAsync(SeededGuest guest)
        {
            Assert.Null(await StoppedAtAsync(guest));
            var states = await _permissions.GetCurrentStatesAsync(guest.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        private async Task<DateTime?> StoppedAtAsync(SeededGuest guest)
        {
            return await _context.MasterGuests
                .AsNoTracking()
                .Where(m => m.Id == guest.MasterGuestId)
                .Select(m => m.SmsStoppedAtUtc)
                .SingleAsync();
        }

        /// <summary>
        /// Seeds one restaurant with one guest. <paramref name="touch"/> adds an SMS
        /// sent from that restaurant to the guest.
        /// </summary>
        private async Task<SeededGuest> SeedGuestAsync(
            string restaurantName,
            string mobile,
            SmsTouch touch,
            string? normalizedPhone = null
        )
        {
            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            // Production Guest Form stores digits-only NormalizedPhone.
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = mobile,
                NormalizedPhone = normalizedPhone
                    ?? new string(mobile.Where(char.IsDigit).ToArray()),
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                MasterGuest = master,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            _permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)
            );
            await _context.SaveChangesAsync();

            if (touch == SmsTouch.FeedbackReply)
            {
                await AddSmsReplyAsync(restaurant.Id, location.Id, guest.Id, mobile);
            }
            else if (touch == SmsTouch.CampaignSend)
            {
                await AddCampaignSmsAsync(
                    location.Id,
                    guest.Id,
                    CampaignFireService.AcceptedOutcome,
                    acceptedAtUtc: _now,
                    providerMessageId: "SMtest-campaign"
                );
            }
            else if (touch == SmsTouch.CampaignSendUnknownOutcome)
            {
                // Outcome other than skipped, no accepted time: the send may have reached the phone.
                await AddCampaignSmsAsync(
                    location.Id,
                    guest.Id,
                    CampaignFireService.RejectedOutcome,
                    acceptedAtUtc: null,
                    providerMessageId: null
                );
            }
            else if (touch == SmsTouch.CampaignSendSkippedIneligible)
            {
                await AddCampaignSmsAsync(
                    location.Id,
                    guest.Id,
                    CampaignFireService.SkippedIneligibleOutcome,
                    acceptedAtUtc: null,
                    providerMessageId: null
                );
            }

            return new SeededGuest(master.Id, guest.Id);
        }

        private async Task AddSmsReplyAsync(
            int restaurantId,
            int locationId,
            int locationGuestId,
            string mobile
        )
        {
            var feedback = new Feedback
            {
                RestaurantLocationId = locationId,
                QrCodeId = 1,
                LocationGuestId = locationGuestId,
                GuestName = "Guest",
                GuestContact = mobile,
                ContactType = ContactType.Phone,
                Comment = "Lovely",
                ClassificationStatus = ClassificationStatus.Succeeded,
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                CreatedAt = _now,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            _context.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = feedback.Id,
                    Channel = FeedbackGuestResponseChannel.Sms,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    MaskedDestination = "+44 •••",
                    Body = "Thank you.",
                    AuthorDisplayName = "Owner",
                    CreatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task AddCampaignSmsAsync(
            int locationId,
            int locationGuestId,
            string outcome,
            DateTime? acceptedAtUtc,
            string? providerMessageId
        )
        {
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Name = "Campaign",
                Status = "sent",
                Channel = "sms",
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            _context.CampaignRecipientDeliveries.Add(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = locationGuestId,
                    Channel = "sms",
                    Outcome = outcome,
                    AcceptedAtUtc = acceptedAtUtc,
                    ProviderMessageId = providerMessageId,
                }
            );
            await _context.SaveChangesAsync();
        }

        public void Dispose() => _context.Dispose();

        private enum SmsTouch
        {
            None,
            FeedbackReply,
            CampaignSend,
            CampaignSendUnknownOutcome,
            CampaignSendSkippedIneligible,
        }

        private sealed record SeededGuest(int MasterGuestId, int LocationGuestId);
    }
}
