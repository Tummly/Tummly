using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services;

public class BulkRecoveryBatchServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FakeFeedbackGuestResponsesService _guestResponses = new();
    private readonly BulkRecoveryBatchService _sut;
    private readonly DateTime _now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    public BulkRecoveryBatchServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
            )
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new BulkRecoveryBatchService(
            _context,
            _guestResponses,
            new FixedTimeProvider(_now)
        );
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateDraft_PersonalisesTemplate_AndSendSubstitutesNames()
    {
        var (restaurantId, locationId, userId) = await SeedShellAsync();
        var james = await SeedFeedbackAsync(
            locationId,
            "James Patel",
            "james@example.com"
        );
        var maya = await SeedFeedbackAsync(
            locationId,
            "Maya Collins",
            "maya@example.com"
        );

        var draft = await _sut.CreateDraftAsync(
            restaurantId,
            locationId,
            userId,
            [james, maya],
            FeedbackGuestResponseChannel.Email,
            FeedbackRecoveryIntent.RespondToGuest,
            "Sorry about Main",
            "Hi James, we are sorry about your visit.",
            "apologise_and_confirm_follow_up",
            "warm_and_apologetic",
            null
        );

        Assert.NotNull(draft);
        Assert.Equal(2, draft!.MemberCount);
        Assert.Contains(
            BulkRecoveryPersonalise.GuestFirstNameToken,
            draft.BodyTemplate,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("Hi James,", draft.BodyTemplate, StringComparison.Ordinal);

        var send = await _sut.SendAsync(draft.Id, userId);
        Assert.NotNull(send);
        Assert.Equal(2, send!.Sent);
        Assert.Equal("completed", send.Status);
        Assert.Equal(2, _guestResponses.Sent.Count);
        Assert.Contains(
            _guestResponses.Sent,
            row => row.Body.StartsWith("Hi James,", StringComparison.Ordinal)
        );
        Assert.Contains(
            _guestResponses.Sent,
            row => row.Body.StartsWith("Hi Maya,", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task Send_PartialWhenOneFails()
    {
        var (restaurantId, locationId, userId) = await SeedShellAsync();
        var a = await SeedFeedbackAsync(locationId, "Ada Lovelace", "ada@example.com");
        var b = await SeedFeedbackAsync(locationId, "Bob Builder", "bob@example.com");
        _guestResponses.FailFeedbackIds.Add(b);

        var draft = await _sut.CreateDraftAsync(
            restaurantId,
            locationId,
            userId,
            [a, b],
            FeedbackGuestResponseChannel.Email,
            FeedbackRecoveryIntent.RespondToGuest,
            null,
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken}, thanks.",
            null,
            null,
            null
        );
        Assert.NotNull(draft);

        var send = await _sut.SendAsync(draft!.Id, userId);
        Assert.NotNull(send);
        Assert.Equal(1, send!.Sent);
        Assert.Equal(1, send.Failed);
        Assert.Equal("partial", send.Status);
    }

    [Fact]
    public async Task Send_DuplicateRowIsRecordedPerRow_OtherRowsStillSend()
    {
        var (restaurantId, locationId, userId) = await SeedShellAsync();
        var a = await SeedFeedbackAsync(locationId, "Ada Lovelace", "ada@example.com");
        var b = await SeedFeedbackAsync(locationId, "Bob Builder", "bob@example.com");
        var c = await SeedFeedbackAsync(locationId, "Cy Young", "cy@example.com");
        _guestResponses.DuplicateFeedbackIds.Add(b);

        var draft = await _sut.CreateDraftAsync(
            restaurantId,
            locationId,
            userId,
            [a, b, c],
            FeedbackGuestResponseChannel.Email,
            FeedbackRecoveryIntent.RespondToGuest,
            null,
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken}, thanks.",
            null,
            null,
            null
        );
        Assert.NotNull(draft);

        var send = await _sut.SendAsync(draft!.Id, userId);
        Assert.NotNull(send);
        Assert.Equal(2, send!.Sent);
        Assert.Equal(1, send.Failed);
        Assert.Equal("partial", send.Status);

        var member = await _context.BulkRecoveryBatchMembers
            .AsNoTracking()
            .SingleAsync(m => m.BatchId == draft.Id && m.FeedbackId == b);
        Assert.Equal(BulkRecoveryBatchMemberStatus.Failed, member.Status);
        Assert.Equal(
            "This reply was already sent to the guest.",
            member.FailureReason
        );
    }

    [Fact]
    public async Task BulkRecovery_StoppedRow_ReportedPerRow_OthersSend()
    {
        var (restaurantId, locationId, userId) = await SeedShellAsync();
        var a = await SeedSmsFeedbackAsync(locationId, "Ada Lovelace", "+447700900201");
        var b = await SeedSmsFeedbackAsync(locationId, "Bob Builder", "+447700900202");
        var c = await SeedSmsFeedbackAsync(locationId, "Cy Young", "+447700900203");
        _guestResponses.SmsStoppedFeedbackIds.Add(b);

        var draft = await _sut.CreateDraftAsync(
            restaurantId,
            locationId,
            userId,
            [a, b, c],
            FeedbackGuestResponseChannel.Sms,
            FeedbackRecoveryIntent.RespondToGuest,
            null,
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken}, thanks.",
            null,
            null,
            null
        );
        Assert.NotNull(draft);

        var send = await _sut.SendAsync(draft!.Id, userId);
        Assert.NotNull(send);
        Assert.Equal(2, send!.Sent);
        Assert.Equal(1, send.Failed);
        Assert.Equal("partial", send.Status);

        var member = await _context.BulkRecoveryBatchMembers
            .AsNoTracking()
            .SingleAsync(m => m.BatchId == draft.Id && m.FeedbackId == b);
        Assert.Equal(BulkRecoveryBatchMemberStatus.Failed, member.Status);
        Assert.Equal(
            "This message cannot be sent for the selected purpose or recipient. Review the reason.",
            member.FailureReason
        );
    }

    private async Task<(int RestaurantId, int LocationId, int UserId)> SeedShellAsync()
    {
        var user = new User
        {
            FullName = "Owner",
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var restaurant = new Restaurant
        {
            Name = "Test Venue",
            OwnerUserId = user.Id,
        };
        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        var location = new RestaurantLocation
        {
            RestaurantId = restaurant.Id,
            LocationName = "Main",
        };
        _context.RestaurantLocations.Add(location);
        await _context.SaveChangesAsync();

        return (restaurant.Id, location.Id, user.Id);
    }

    private async Task<int> SeedSmsFeedbackAsync(int locationId, string guestName, string mobile)
    {
        var restaurantId = await _context.RestaurantLocations
            .Where(l => l.Id == locationId)
            .Select(l => l.RestaurantId)
            .FirstAsync();
        var master = new MasterGuest
        {
            RestaurantId = restaurantId,
            Mobile = mobile,
            NormalizedPhone = new string(mobile.Where(char.IsDigit).ToArray()),
            CreatedAt = _now,
        };
        _context.MasterGuests.Add(master);
        await _context.SaveChangesAsync();

        var locationGuest = new LocationGuest
        {
            MasterGuestId = master.Id,
            RestaurantLocationId = locationId,
            Name = guestName,
            MarketingPreference = LocationGuestMarketingPreference.Allowed,
            CreatedAt = _now,
        };
        _context.LocationGuests.Add(locationGuest);
        await _context.SaveChangesAsync();

        var feedback = new Feedback
        {
            RestaurantLocationId = locationId,
            QrCodeId = 1,
            LocationGuestId = locationGuest.Id,
            GuestName = guestName,
            GuestContact = mobile,
            ContactType = ContactType.Phone,
            Comment = "Needs attention",
            ClassificationStatus = ClassificationStatus.Succeeded,
            Sentiment = FeedbackSentiment.Negative,
            WorkflowStatus = FeedbackWorkflowStatus.New,
            CreatedAt = _now.AddDays(-1),
        };
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return feedback.Id;
    }

    private async Task<int> SeedFeedbackAsync(
        int locationId,
        string guestName,
        string email
    )
    {
        var restaurantId = await _context.RestaurantLocations
            .Where(l => l.Id == locationId)
            .Select(l => l.RestaurantId)
            .FirstAsync();
        var master = new MasterGuest
        {
            RestaurantId = restaurantId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            CreatedAt = _now,
        };
        _context.MasterGuests.Add(master);
        await _context.SaveChangesAsync();

        var locationGuest = new LocationGuest
        {
            MasterGuestId = master.Id,
            RestaurantLocationId = locationId,
            Name = guestName,
            MarketingPreference = LocationGuestMarketingPreference.Allowed,
            CreatedAt = _now,
        };
        _context.LocationGuests.Add(locationGuest);
        await _context.SaveChangesAsync();

        var feedback = new Feedback
        {
            RestaurantLocationId = locationId,
            QrCodeId = 1,
            LocationGuestId = locationGuest.Id,
            GuestName = guestName,
            GuestContact = email,
            ContactType = ContactType.Email,
            Comment = "Needs attention",
            ClassificationStatus = ClassificationStatus.Succeeded,
            Sentiment = FeedbackSentiment.Negative,
            WorkflowStatus = FeedbackWorkflowStatus.New,
            CreatedAt = _now.AddDays(-1),
        };
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return feedback.Id;
    }
}
