using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class BillingActivityCopyFormatterTests
    {
        [Fact]
        public void FormatSentence_MatchesFrontend08Copy_ForCreditConsumedCampaign()
        {
            var sentence = BillingActivityCopyFormatter.FormatSentence(
                new RestaurantBillingActivity
                {
                    Kind = BillingActivityKinds.CreditConsumed,
                    Channel = CreditChannels.Sms,
                    Qty = 212,
                    CampaignName = "Quiet Tuesday Boost",
                    ConsumeSource = "campaign",
                }
            );

            Assert.Equal(
                "212 SMS credits used by Quiet Tuesday Boost.",
                sentence
            );
        }

        [Fact]
        public void FormatSentence_MatchesFrontend08Copy_ForFeedbackRecovery()
        {
            var sentence = BillingActivityCopyFormatter.FormatSentence(
                new RestaurantBillingActivity
                {
                    Kind = BillingActivityKinds.CreditConsumed,
                    Channel = CreditChannels.Sms,
                    Qty = 1,
                    ConsumeSource = "feedback_recovery",
                }
            );

            Assert.Equal("1 SMS credit used by Feedback recovery.", sentence);
        }

        [Fact]
        public void FormatSentence_MatchesFrontend08Copy_ForTopupPurchased()
        {
            var sentence = BillingActivityCopyFormatter.FormatSentence(
                new RestaurantBillingActivity
                {
                    Kind = BillingActivityKinds.TopupPurchased,
                    Channel = CreditChannels.Sms,
                    Qty = 1000,
                    ActorDisplayName = "James Cole",
                }
            );

            Assert.Equal("1,000 SMS credits added by James Cole.", sentence);
        }
    }
}
