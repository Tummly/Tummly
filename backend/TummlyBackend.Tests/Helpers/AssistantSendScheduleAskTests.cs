using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantSendScheduleAskTests
    {
        [Theory]
        [InlineData("send it now")]
        [InlineData("Send it now")]
        [InlineData("please send now")]
        [InlineData("schedule this campaign")]
        [InlineData("schedule it for Friday")]
        [InlineData("Show me Campaign drafts and send it now")]
        public void LooksLikeSendOrSchedule_IsTrue(string message)
        {
            Assert.True(AssistantSendScheduleAsk.LooksLikeSendOrSchedule(message));
        }

        [Theory]
        [InlineData("Show me Campaign drafts")]
        [InlineData("Summarise scheduled campaigns")]
        [InlineData("Draft an Email Campaign to bring back all currently Email-eligible guests at Camden")]
        [InlineData("Prepare a recovery response")]
        public void LooksLikeSendOrSchedule_IsFalse(string message)
        {
            Assert.False(AssistantSendScheduleAsk.LooksLikeSendOrSchedule(message));
        }

        [Theory]
        [InlineData("activate this offer")]
        [InlineData("activate the Offer")]
        [InlineData("issue an offer")]
        [InlineData("redeem the offer")]
        public void LooksLikeOfferActivate_IsTrue(string message)
        {
            Assert.True(AssistantSendScheduleAsk.LooksLikeOfferActivate(message));
        }

        [Fact]
        public void Classify_SendItNowWithRetrieve_IsRefuse()
        {
            Assert.Equal(
                AssistantTask.Refuse,
                AssistantTaskClassification.Classify(
                    "Show me Campaign drafts and send it now"
                )
            );
        }

        [Fact]
        public void Classify_CanonicalCreatePlusSendItNow_StaysCreate()
        {
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify(
                    "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden and send it now"
                )
            );
        }

        [Fact]
        public void Landing_SendItNow_IsReviewSendNow()
        {
            var landing = AssistantSendScheduleAsk.CampaignLanding(
                "send it now",
                new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(AssistantSendScheduleAsk.StepReview, landing.Step);
            Assert.Equal(AssistantSendScheduleAsk.ModeSendNow, landing.ScheduleMode);
            Assert.Null(landing.DateLocal);
            Assert.Null(landing.TimeLocal);
        }

        [Fact]
        public void Landing_ScheduleWithoutDatetime_IsScheduleStep()
        {
            var landing = AssistantSendScheduleAsk.CampaignLanding(
                "schedule this campaign",
                new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(AssistantSendScheduleAsk.StepSchedule, landing.Step);
            Assert.Equal(AssistantSendScheduleAsk.ModeScheduleLater, landing.ScheduleMode);
            Assert.Null(landing.DateLocal);
            Assert.Null(landing.TimeLocal);
        }

        [Fact]
        public void Landing_ScheduleWithFutureDatetime_IsReviewLaterPrefill()
        {
            var landing = AssistantSendScheduleAsk.CampaignLanding(
                "schedule this campaign for 21 August 2027 at 18:00",
                new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(AssistantSendScheduleAsk.StepReview, landing.Step);
            Assert.Equal(AssistantSendScheduleAsk.ModeScheduleLater, landing.ScheduleMode);
            Assert.Equal("2027-08-21", landing.DateLocal);
            Assert.Equal("18:00", landing.TimeLocal);
        }

        [Fact]
        public void Landing_PastOrIncompleteDatetime_IsScheduleWithoutPrefill()
        {
            var now = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);
            var past = AssistantSendScheduleAsk.CampaignLanding(
                "schedule this campaign for 10 August 2026 at 09:00",
                now
            );
            var incomplete = AssistantSendScheduleAsk.CampaignLanding(
                "schedule this campaign for 21 August 2027",
                now
            );

            Assert.Equal(AssistantSendScheduleAsk.StepSchedule, past.Step);
            Assert.Null(past.DateLocal);
            Assert.Null(past.TimeLocal);
            Assert.Equal(AssistantSendScheduleAsk.StepSchedule, incomplete.Step);
            Assert.Null(incomplete.DateLocal);
            Assert.Null(incomplete.TimeLocal);
        }

        [Fact]
        public void Landing_AmbiguousSendOrSchedule_IsScheduleStep()
        {
            var landing = AssistantSendScheduleAsk.CampaignLanding(
                "send or schedule this campaign",
                new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(AssistantSendScheduleAsk.StepSchedule, landing.Step);
            Assert.Null(landing.DateLocal);
            Assert.Null(landing.TimeLocal);
        }

        [Fact]
        public void NamedMismatch_DifferentCampaignName_IsTrue()
        {
            Assert.True(
                AssistantSendScheduleAsk.IsNamedCampaignMismatch(
                    "send the Lunch reminder campaign",
                    "Bring back Email-eligible guests at Camden"
                )
            );
        }

        [Fact]
        public void NamedMismatch_SendItNow_IsFalse()
        {
            Assert.False(
                AssistantSendScheduleAsk.IsNamedCampaignMismatch(
                    "send it now",
                    "Bring back Email-eligible guests at Camden"
                )
            );
        }

        [Fact]
        public void StoredCampaign_WithRecoveryScheduleLanguage_DoesNotUseCampaignLanding()
        {
            Assert.True(
                AssistantSendScheduleAsk.LooksLikeTimedSchedule("schedule it for Friday")
            );
        }
    }
}
