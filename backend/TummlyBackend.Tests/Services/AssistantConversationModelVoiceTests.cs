using Microsoft.EntityFrameworkCore;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_FeedbackExplanation_KeepsModelBody()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );
            const string modelBody =
                "The one Feedback item this week is negative and mentions wait time. Follow up with that guest before you draft a recovery.";
            _fake.ExecuteToolsBeforeForcedResult = true;
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Wait time",
                modelBody,
                AssistantTask.Retrieve
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How is Feedback looking this week?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal("Wait time", answer.Title);
            Assert.Equal(modelBody, answer.Body);
        }

        [Fact]
        public async Task SendTurn_ProductHelp_KeepsModelBody()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            const string modelBody =
                "I can explain Feedback, offers, and Campaigns, then draft the next one when you name the location.";
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "How I can help",
                modelBody,
                AssistantTask.Retrieve,
                "Help with the assistant"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What can you do")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal(modelBody, answer.Body);
            Assert.Equal("How I can help", answer.Title);
            Assert.DoesNotContain(
                AssistantProductExpertCopy.CapabilitiesBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Theory]
        [InlineData("30 days", CatalogOfferValidity.Days30AfterIssue)]
        [InlineData("two weeks", CatalogOfferValidity.Days14AfterIssue)]
        [InlineData("a fortnight", CatalogOfferValidity.Days14AfterIssue)]
        [InlineData("next week", CatalogOfferValidity.Days7AfterIssue)]
        [InlineData("end of the month", CatalogOfferValidity.ChooseExpiryDate)]
        [InlineData("next Friday", CatalogOfferValidity.ChooseExpiryDate)]
        public async Task SendTurn_OfferExpiryFollowUp_KeepsTheOffer(
            string reply,
            CatalogOfferValidity validity
        )
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Clarify,
                null,
                "When should diners be able to use their discount?",
                AssistantTask.OfferPath,
                null,
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                    DiscountPercentage = 25m,
                }
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create a 25% off lunch offer")
                )
            );
            Assert.Equal(AssistantGapAsk.EndDateAsk, started.Conversation.Messages[^1].Body);

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, reply, started.Conversation.Id)
                )
            );

            var body = answered.Conversation.Messages[^1].Body;
            Assert.DoesNotContain(
                AssistantGapAsk.PreviousDraftDropped,
                body,
                StringComparison.Ordinal
            );
            Assert.NotEqual("failure", answered.Conversation.Messages[^1].Class);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Equal(validity, offer.Validity);
            Assert.Null(await StoredGapStateOrNullAsync(started.Conversation.Id));
        }

        [Fact]
        public async Task SendTurn_OfferExpiryFollowUp_UnparsedDate_SendsPriorThreadToTheModel()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var ask = "Create a 25% off lunch offer";
            _fake.EnqueueSucceedWith(
                AssistantMessageClass.Clarify,
                null,
                "Until what date should the offer run?",
                AssistantTask.OfferPath,
                null,
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                    DiscountPercentage = 25m,
                }
            );
            _fake.EnqueueSucceedWith(
                AssistantMessageClass.Grounded,
                "Lunch offer",
                "Saving the lunch offer.",
                AssistantTask.Retrieve,
                "Create Offer Draft"
            );
            _fake.EnqueueSucceedWith(
                AssistantMessageClass.Grounded,
                "Lunch offer",
                "The 25% lunch offer ends on the day you named.",
                AssistantTask.OfferPath,
                null,
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                    DiscountPercentage = 25m,
                    Validity = "choose_expiry_date",
                    ExpiryDate = "2026-10-16",
                }
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, ask)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "the day we discussed",
                        started.Conversation.Id
                    )
                )
            );

            Assert.NotNull(_fake.LastInput);
            Assert.Contains(_fake.LastInput!.History!, turn => turn.Body == ask);
            Assert.Contains(
                _fake.LastInput.History!,
                turn => turn.Body == AssistantGapAsk.EndDateAsk
            );
            Assert.Equal("the day we discussed", _fake.LastInput.UserMessage);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Equal(CatalogOfferValidity.ChooseExpiryDate, offer.Validity);
            Assert.Equal(new DateOnly(2026, 10, 16), offer.CustomExpiryDate);
            Assert.Equal("grounded", answered.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_FeedbackFollowUp_SendsThePreviousAnswerAsHistory()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );
            var ask = "Summarise recent feedback";

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, ask)
                )
            );
            var firstAnswer = started.Conversation.Messages[^1].Body;

            var followed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Why is the feedback like that?",
                        started.Conversation.Id
                    )
                )
            );

            Assert.NotNull(_fake.LastInput?.History);
            Assert.Contains(_fake.LastInput!.History!, turn => turn.Body == ask);
            Assert.Contains(_fake.LastInput.History!, turn => turn.Body == firstAnswer);
            Assert.Equal("Why is the feedback like that?", _fake.LastInput.UserMessage);
            Assert.NotEqual("failure", followed.Conversation.Messages[^1].Class);
            Assert.DoesNotContain(
                AssistantGapAsk.PreviousDraftDropped,
                followed.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_CampaignChannelFollowUp_KeepsTheOpenDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Draft an Email and SMS Campaign to bring back eligible guests at Camden"
                    )
                )
            );
            Assert.Equal(AssistantGapAsk.ChannelAsk, started.Conversation.Messages[^1].Body);

            var followed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "the guest inbox one",
                        started.Conversation.Id
                    )
                )
            );

            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain(
                AssistantGapAsk.PreviousDraftDropped,
                followed.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Email",
                followed.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "SMS",
                followed.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }
    }
}
