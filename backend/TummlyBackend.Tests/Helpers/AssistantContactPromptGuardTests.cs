using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers;

/// <summary>
/// R2.8 regression lock: raw guest contact never reaches the Assistant model
/// prompt. Evidence rows carry only masked or no contact; the redaction tokens
/// stay server-side.
/// </summary>
public class AssistantContactPromptGuardTests : IDisposable
{
    private const string GuardEmail = "alex.guard@example.com";
    private const string GuardMobile = "07700900321";
    private const int LocationId = 1;
    private const string GuardCommentMarker = "Guard feedback comment";

    private static readonly DateTime WindowFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowTo = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _context;

    public AssistantContactPromptGuardTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)
            )
            .Options;

        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task EvidencePayload_ContainsNoRawContact()
    {
        await SeedGuestWithContactAsync();

        var feedback = Assert.IsType<AssistantFeedbackRetrieveResult.Ok>(
            await new AssistantFeedbackRetrieve(_context).RetrieveAsync(
                LocationId,
                WindowFrom,
                WindowTo
            )
        ).Evidence;
        var guests = Assert.IsType<AssistantGuestsRetrieveResult.Ok>(
            await new AssistantGuestsRetrieve(_context).RetrieveAsync(LocationId)
        ).Evidence;

        // The seed must reach the retrieve layer, or the guard proves nothing.
        Assert.Contains(GuardEmail, feedback.ContactRedactionTokens);
        Assert.Contains(GuardMobile, feedback.ContactRedactionTokens);
        Assert.Contains(GuardEmail, guests.ContactRedactionTokens);
        Assert.Contains(GuardMobile, guests.ContactRedactionTokens);

        var evidence = AssistantRetrievedEvidence.Empty with
        {
            Feedback = feedback,
            Guests = guests,
        };

        var json = AssistantLiveAnswerStructuredOutput
            .BuildEvidencePayload(evidence)
            .ToJsonString();

        Assert.Contains(GuardCommentMarker, json, StringComparison.Ordinal);
        Assert.DoesNotContain(GuardEmail, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(GuardMobile, json, StringComparison.Ordinal);
    }

    private async Task SeedGuestWithContactAsync()
    {
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var master = new MasterGuest
        {
            RestaurantId = 1,
            Email = GuardEmail,
            NormalizedEmail = GuardEmail,
            Mobile = GuardMobile,
            NormalizedPhone = GuardMobile,
            CreatedAt = now,
        };
        _context.MasterGuests.Add(master);
        await _context.SaveChangesAsync();

        var guest = new LocationGuest
        {
            MasterGuestId = master.Id,
            RestaurantLocationId = LocationId,
            Name = "Alex Guard",
            MarketingPreference = LocationGuestMarketingPreference.Allowed,
            CreatedAt = now,
        };
        _context.LocationGuests.Add(guest);
        await _context.SaveChangesAsync();

        _context.Feedbacks.AddRange(
            new Feedback
            {
                RestaurantLocationId = LocationId,
                QrCodeId = 1,
                LocationGuestId = guest.Id,
                GuestName = "Alex Guard",
                GuestContact = GuardEmail,
                ContactType = ContactType.Email,
                Comment = GuardCommentMarker,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.New,
                CreatedAt = now,
            },
            new Feedback
            {
                RestaurantLocationId = LocationId,
                QrCodeId = 1,
                GuestName = "Phone Guard",
                GuestContact = GuardMobile,
                ContactType = ContactType.Phone,
                Comment = "Guard phone comment",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Neutral,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.New,
                CreatedAt = now.AddMinutes(5),
            }
        );
        await _context.SaveChangesAsync();
    }
}
