using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRecoveryIdentityTests
    {
        private const string LastNegativeOnThisLocation =
            "Create a recovery offer for the last negative feedback we recieved on this location";

        [Fact]
        public void LastNegativeAsk_DoesNotTreatThePhraseAsAGuestName()
        {
            var older = Row(11, "Pat Guest", "negative", hoursAgo: 5);
            var newer = Row(12, "Alex Guest", "negative", hoursAgo: 1);

            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [older, newer]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(12, one.Row.Id);
        }

        [Fact]
        public void LastNegativeAsk_BindsResolvedNegative()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [Row(11, "Pat Guest", "negative", hoursAgo: 2, workflow: "Resolved")]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(11, one.Row.Id);
        }

        [Fact]
        public void UnnamedAsk_WithTwoRows_IsStillAGap()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "Prepare a recovery response",
                [
                    Row(11, "Pat Guest", "negative", hoursAgo: 1),
                    Row(12, "Alex Guest", "negative", hoursAgo: 2),
                ]
            );

            Assert.IsType<AssistantRecoveryIdentity.Match.Many>(match);
        }

        [Fact]
        public void LastNegativeAsk_WithOnlyPositiveFeedback_IsNoNegative()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                LastNegativeOnThisLocation,
                [Row(11, "Pat Guest", "positive", hoursAgo: 1)]
            );

            var none = Assert.IsType<AssistantRecoveryIdentity.Match.None>(match);
            Assert.Equal(AssistantRecoveryIdentity.ReasonNoNegative, none.Reason);
        }

        [Fact]
        public void FormatLabel_WithVenue_UsesGuestDateAndVenue()
        {
            var row = Row(
                11,
                "Pat Guest",
                "negative",
                hoursAgo: 1,
                locationName: "Camden",
                createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Pat Guest (1 Jan 12:00) · Camden",
                AssistantRecoveryIdentity.FormatLabel(row, includeVenue: true)
            );
        }

        [Fact]
        public void FormatLabel_WithoutVenue_OmitsVenue()
        {
            var row = Row(
                11,
                "Pat Guest",
                "negative",
                hoursAgo: 1,
                locationName: "Camden",
                createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            );

            Assert.Equal(
                "Pat Guest (1 Jan 12:00)",
                AssistantRecoveryIdentity.FormatLabel(row)
            );
        }

        [Fact]
        public void GapBody_WithVenue_ListsCollidingRowsWithVenue()
        {
            var camden = Row(
                11,
                "Pat Guest",
                "negative",
                hoursAgo: 2,
                locationName: "Camden"
            );
            var soho = Row(
                12,
                "Alex Guest",
                "negative",
                hoursAgo: 1,
                locationName: "Soho"
            );

            var body = AssistantRecoveryIdentity.GapBody(
                [camden, soho],
                includeVenue: true
            );

            Assert.Contains("Pat Guest", body, StringComparison.Ordinal);
            Assert.Contains("Camden", body, StringComparison.Ordinal);
            Assert.Contains("Alex Guest", body, StringComparison.Ordinal);
            Assert.Contains("Soho", body, StringComparison.Ordinal);
            Assert.Contains(" · ", body, StringComparison.Ordinal);
        }

        [Fact]
        public void SameGuestSameDay_ChoiceLabelsIncludeTime()
        {
            var morning = Row(
                11,
                "Salman Shahid",
                "negative",
                hoursAgo: 5,
                createdAt: new DateTime(2026, 10, 5, 9, 15, 0, DateTimeKind.Utc),
                excerpt: "Food was cold"
            );
            var afternoon = Row(
                12,
                "Salman Shahid",
                "negative",
                hoursAgo: 2,
                createdAt: new DateTime(2026, 10, 5, 16, 40, 0, DateTimeKind.Utc),
                excerpt: "Slow service"
            );

            var labels = AssistantRecoveryIdentity.ChoiceLabels([morning, afternoon]);

            Assert.Equal(2, labels.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal("Salman Shahid (5 Oct 09:15)", labels[0]);
            Assert.Equal("Salman Shahid (5 Oct 16:40)", labels[1]);
            Assert.DoesNotContain("2026", string.Join('|', labels), StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Salman Shahid (5 Oct 09:15), Salman Shahid (5 Oct 09:15)",
                AssistantRecoveryIdentity.GapBody([morning, afternoon]),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void GapBody_UsesBulletsAndOmitsFdbCodes()
        {
            var rows = SameDayRows();
            var body = AssistantRecoveryIdentity.GapBody(rows);

            Assert.Contains("Which Feedback should I recover:", body, StringComparison.Ordinal);
            Assert.Contains("\n- ", body, StringComparison.Ordinal);
            Assert.DoesNotContain("FDB-", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(", Salman", body, StringComparison.Ordinal);
        }

        [Fact]
        public void Yes_WithOneFeedback_BindsThatRow()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "yes",
                [Row(11, "Salman Shahid", "negative", hoursAgo: 1)]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(11, one.Row.Id);
        }

        [Fact]
        public void Yes_WithThreeSameDayRows_BindsNewestForRecovery()
        {
            var rows = SameDayRows();
            var match = AssistantRecoveryIdentity.Resolve("yes", rows);

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(13, one.Row.Id);
            Assert.Contains("16:40", AssistantRecoveryIdentity.FormatLabel(one.Row), StringComparison.Ordinal);
        }

        [Fact]
        public void AllNegativeFeedbacks_SaysOneAtATime_WithDistinctRows()
        {
            var rows = SameDayRows();
            var match = AssistantRecoveryIdentity.Resolve(
                "all negative feedbacks",
                rows
            );

            Assert.IsType<AssistantRecoveryIdentity.Match.Many>(match);
            var body = AssistantRecoveryIdentity.ReplyBody(
                "all negative feedbacks",
                rows
            );
            Assert.Contains(
                "I can recover one Feedback at a time",
                body,
                StringComparison.Ordinal
            );
            Assert.Contains("09:15", body, StringComparison.Ordinal);
            Assert.Contains("16:40", body, StringComparison.Ordinal);
        }

        [Fact]
        public void AllNegativeFeedbacks_WithOneNegative_BindsThatRow()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "all negative feedbacks",
                [
                    Row(11, "Salman Shahid", "negative", hoursAgo: 1),
                    Row(12, "Salman Shahid", "positive", hoursAgo: 2),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(11, one.Row.Id);
        }

        [Fact]
        public void NamedGuestMiss_IsStillNone()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "Prepare a recovery response for Mehmet",
                [Row(11, "Pat Guest", "negative", hoursAgo: 1)]
            );

            var none = Assert.IsType<AssistantRecoveryIdentity.Match.None>(match);
            Assert.Equal(AssistantRecoveryIdentity.ReasonNamedMiss, none.Reason);
        }

        [Fact]
        public void BareGuestName_BindsUniqueRow()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "Mia Khan",
                [
                    Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                    Row(98, "James Patel", "neutral", hoursAgo: 2),
                    Row(103, "Mia Khan", "positive", hoursAgo: 6),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(103, one.Row.Id);
        }

        [Fact]
        public void Yes_OnIndistinctDuplicateLabels_BindsNewest()
        {
            var stamp = new DateTime(2026, 10, 8, 5, 55, 0, DateTimeKind.Utc);
            var older = Row(
                103,
                "Mia Khan",
                "positive",
                hoursAgo: 6,
                createdAt: stamp,
                excerpt: "Great atmosphere and a quick turnaround."
            );
            var newer = Row(
                203,
                "Mia Khan",
                "positive",
                hoursAgo: 6,
                createdAt: stamp,
                excerpt: "Great atmosphere and a quick turnaround."
            );

            var match = AssistantRecoveryIdentity.Resolve("yes", [older, newer]);

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(203, one.Row.Id);
        }

        [Fact]
        public void Yes_WithSeveralGuests_BindsNewest_NotReask()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "yes",
                [
                    Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                    Row(98, "James Patel", "neutral", hoursAgo: 2),
                    Row(99, "Priya Nguyen", "negative", hoursAgo: 3),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(97, one.Row.Id);
        }

        [Fact]
        public void MakeIt_BindsNewestLikeConfirm()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "make it",
                [
                    Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                    Row(99, "Priya Nguyen", "negative", hoursAgo: 3),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(97, one.Row.Id);
            Assert.True(AssistantRecoveryIdentity.IsShortConfirm("make it"));
        }

        [Fact]
        public void NaturalPrepareRecovery_BindsNewest()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "please prepare a recovery",
                [
                    Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                    Row(99, "Priya Nguyen", "negative", hoursAgo: 3),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(97, one.Row.Id);
        }

        [Fact]
        public void NaturalRecoverNamedGuest_BindsThatGuest()
        {
            var match = AssistantRecoveryIdentity.Resolve(
                "recover Priya Nguyen",
                [
                    Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                    Row(99, "Priya Nguyen", "negative", hoursAgo: 3),
                ]
            );

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(99, one.Row.Id);
        }

        [Fact]
        public void OptionNumber_PicksThatRow()
        {
            var rows = new[]
            {
                Row(97, "Walk-in 61", "positive", hoursAgo: 1),
                Row(98, "James Patel", "neutral", hoursAgo: 2),
                Row(99, "Priya Nguyen", "negative", hoursAgo: 3),
            };

            var match = AssistantRecoveryIdentity.Resolve("#2", rows);

            var one = Assert.IsType<AssistantRecoveryIdentity.Match.One>(match);
            Assert.Equal(98, one.Row.Id);
        }

        [Fact]
        public void PrepareRecoveryResponses_AsksForSetPrepare()
        {
            Assert.True(
                AssistantRecoveryIdentity.AsksForSetPrepare(
                    "Prepare recovery responses for the guests that need attention in this Feedback set."
                )
            );
            Assert.True(
                AssistantRecoveryIdentity.AsksToSendBulk("Send this recovery to all 3")
            );
            Assert.True(AssistantRecoveryIdentity.AsksToSendBulk("no changes"));
        }

        [Fact]
        public void ForBoth_AsksForAll_OnNamedPool()
        {
            var stamp = new DateTime(2026, 10, 8, 5, 55, 0, DateTimeKind.Utc);
            var a = Row(
                103,
                "Mia Khan",
                "positive",
                hoursAgo: 6,
                createdAt: stamp,
                excerpt: "Great atmosphere and a quick turnaround."
            );
            var b = Row(
                203,
                "Mia Khan",
                "positive",
                hoursAgo: 6,
                createdAt: stamp,
                excerpt: "Great atmosphere and a quick turnaround."
            );

            var match = AssistantRecoveryIdentity.Resolve("for both", [a, b]);

            var many = Assert.IsType<AssistantRecoveryIdentity.Match.Many>(match);
            Assert.Equal(2, many.Rows.Count);
            Assert.True(AssistantRecoveryIdentity.AsksForAll("for both"));
        }

        [Fact]
        public void ChoiceLabels_NumbersIndistinctDuplicates()
        {
            var stamp = new DateTime(2026, 10, 8, 5, 55, 0, DateTimeKind.Utc);
            var labels = AssistantRecoveryIdentity.ChoiceLabels(
                [
                    Row(
                        103,
                        "Mia Khan",
                        "positive",
                        hoursAgo: 6,
                        createdAt: stamp,
                        excerpt: "Great atmosphere and a quick turnaround."
                    ),
                    Row(
                        203,
                        "Mia Khan",
                        "positive",
                        hoursAgo: 6,
                        createdAt: stamp,
                        excerpt: "Great atmosphere and a quick turnaround."
                    ),
                ]
            );

            Assert.Equal(2, labels.Distinct(StringComparer.Ordinal).Count());
            Assert.Contains("#1", labels[0], StringComparison.Ordinal);
            Assert.Contains("#2", labels[1], StringComparison.Ordinal);
        }

        [Fact]
        public void RowsForGapOptions_KeepsOnlyOfferedRows()
        {
            var mia = Row(103, "Mia Khan", "positive", hoursAgo: 6);
            var other = Row(97, "Walk-in 61", "positive", hoursAgo: 1);
            var options = AssistantRecoveryIdentity.ChoiceLabels([mia, other]);
            var narrowed = AssistantRecoveryIdentity.RowsForGapOptions(
                [mia, other, Row(98, "James Patel", "neutral", hoursAgo: 2)],
                [options[0]]
            );

            Assert.Single(narrowed);
            Assert.Equal(103, narrowed[0].Id);
        }

        [Fact]
        public void BareGuestName_WithNoRows_IsNamedMiss()
        {
            var match = AssistantRecoveryIdentity.Resolve("Mia Khan", []);

            var none = Assert.IsType<AssistantRecoveryIdentity.Match.None>(match);
            Assert.Equal(AssistantRecoveryIdentity.ReasonNamedMiss, none.Reason);
        }

        private static IReadOnlyList<AssistantFeedbackEvidenceRow> SameDayRows()
            =>
            [
                Row(
                    11,
                    "Salman Shahid",
                    "negative",
                    hoursAgo: 5,
                    createdAt: new DateTime(2026, 10, 5, 9, 15, 0, DateTimeKind.Utc),
                    excerpt: "Food was cold"
                ),
                Row(
                    12,
                    "Salman Shahid",
                    "negative",
                    hoursAgo: 3,
                    createdAt: new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc),
                    excerpt: "Long wait"
                ),
                Row(
                    13,
                    "Salman Shahid",
                    "negative",
                    hoursAgo: 1,
                    createdAt: new DateTime(2026, 10, 5, 16, 40, 0, DateTimeKind.Utc),
                    excerpt: "Slow service"
                ),
            ];

        private static AssistantFeedbackEvidenceRow Row(
            int id,
            string guestName,
            string? sentiment,
            int hoursAgo,
            string workflow = "New",
            string? locationName = null,
            DateTime? createdAt = null,
            string excerpt = "Slow service"
        )
            => new(
                id,
                createdAt ?? DateTime.UtcNow.AddHours(-hoursAgo),
                guestName,
                sentiment,
                "Succeeded",
                [],
                workflow,
                sentiment == "negative" && workflow != "Resolved",
                null,
                "Email",
                excerpt,
                $"FDB-{id.ToString().PadLeft(6, '0')}",
                null,
                [],
                null,
                false,
                LocationId: null,
                LocationName: locationName
            );
    }
}
