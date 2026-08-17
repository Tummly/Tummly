using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly string ExpectedBaseHeader =
            "Feedback ID,Submitted at,Feedback,Guest response,"
            + "Classification status,Issue tags,Location,Source,"
            + "Workflow status,Needs attention";

        private static readonly string ExpectedContactHeader =
            ExpectedBaseHeader + ",Guest,Email,Mobile";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackExportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Export_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(
                ExportUrl(1, from, to)
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_Returns403_WhenLocationNotOwned()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerWithLocationAsync(
                "feedback-export-owner"
            );
            var other = await SeedOwnerWithLocationAsync(
                "feedback-export-other"
            );

            using var request = AuthorizedGet(
                ExportUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_CurrentCsv_ReturnsColumnsAndRowsWithFilters()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-export-csv"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Cold soup was awful",
                "Alex Soup",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New,
                detectedTagsJson: "[\"Service\"]",
                contactType: ContactType.Email,
                guestContact: "alex@example.com"
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                "Great meal",
                "Blair",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.Resolved
            );

            using var request = AuthorizedGet(
                ExportUrl(
                    seeded.LocationId,
                    from,
                    to,
                    scope: "current",
                    format: "csv",
                    tab: "needs-attention",
                    includeGuestContact: true
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-feedback-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);

            var csv = await response.Content.ReadAsStringAsync();
            var lines = SplitCsvLines(csv);
            Assert.Equal(ExpectedContactHeader, lines[0]);
            Assert.Equal(2, lines.Count);
            Assert.Contains("Cold soup was awful", lines[1]);
            Assert.Contains("Negative", lines[1]);
            Assert.Contains("Service", lines[1]);
            Assert.Contains("New", lines[1]);
            Assert.Contains("Yes", lines[1]);
            Assert.Contains("Alex Soup", lines[1]);
            Assert.Contains("alex@example.com", lines[1]);
            Assert.DoesNotContain("Great meal", csv);
        }

        [Fact]
        public async Task Export_AllInPeriodXlsx_IgnoresTabAndReturnsWorkbook()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-export-xlsx"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Needs attention",
                "Alex",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                "Resolved positive",
                "Blair",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.Resolved
            );

            using var request = AuthorizedGet(
                ExportUrl(
                    seeded.LocationId,
                    from,
                    to,
                    scope: "all-in-period",
                    format: "xlsx",
                    tab: "needs-attention"
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.EndsWith("Z.xlsx", fileName);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var cellTexts = ReadInlineStringsFromXlsx(bytes);
            Assert.Contains("Feedback ID", cellTexts);
            Assert.Contains("Needs attention", cellTexts);
            Assert.Contains("Resolved positive", cellTexts);
        }

        [Fact]
        public async Task Export_FeedbackId_ReturnsOnlyRequestedFeedback()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-export-single"
            );
            var requestedId = await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Requested feedback",
                "Alex",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral,
                FeedbackWorkflowStatus.New
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                "Other feedback",
                "Blair",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.Resolved
            );

            using var request = AuthorizedGet(
                ExportUrl(
                    seeded.LocationId,
                    from,
                    to,
                    format: "csv",
                    feedbackId: requestedId
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("Requested feedback", csv);
            Assert.DoesNotContain("Other feedback", csv);
            Assert.Equal(2, SplitCsvLines(csv).Count);
        }

        [Fact]
        public async Task Export_EmptyScope_Returns400()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-export-empty"
            );

            using var request = AuthorizedGet(
                ExportUrl(
                    seeded.LocationId,
                    from,
                    to,
                    scope: "current",
                    format: "csv"
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "No feedback",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task Export_OverSoftMax_Returns400WithPrdCopy()
        {
            var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-export-cap"
            );

            using var scope = _factory.Services.CreateScope();
            var export = scope.ServiceProvider
                .GetRequiredService<IFeedbackInboxListService>();

            // Seed soft-max+1 via service count path: direct ArgumentException
            // when filtered count exceeds — use oversized in-memory query via
            // ExportAsync after inserting rows in batches.
            await SeedManyFeedbackAsync(
                seeded.LocationId,
                count: FeedbackInboxListService.ExportSoftMaxRows + 1,
                from
            );

            using var request = AuthorizedGet(
                ExportUrl(
                    seeded.LocationId,
                    from,
                    to,
                    scope: "all-in-period",
                    format: "csv"
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                FeedbackInboxListService.ExportSoftMaxMessage,
                body.GetProperty("message").GetString()
            );

            // Keep unused export var referenced for discoverability.
            Assert.NotNull(export);
        }

        private static string ExportUrl(
            int locationId,
            DateTime from,
            DateTime to,
            string scope = "current",
            string format = "xlsx",
            string tab = "all",
            bool includeGuestContact = false,
            int? feedbackId = null
        )
        {
            var builder = new StringBuilder();
            builder.Append("/api/feedback/export?");
            builder.Append($"locationId={locationId}");
            builder.Append($"&from={Uri.EscapeDataString(from.ToString("O"))}");
            builder.Append($"&to={Uri.EscapeDataString(to.ToString("O"))}");
            builder.Append($"&scope={Uri.EscapeDataString(scope)}");
            builder.Append($"&format={Uri.EscapeDataString(format)}");
            builder.Append($"&tab={Uri.EscapeDataString(tab)}");
            if (includeGuestContact)
            {
                builder.Append("&includeGuestContact=true");
            }
            if (feedbackId.HasValue)
            {
                builder.Append($"&feedbackId={feedbackId.Value}");
            }

            return builder.ToString();
        }

        private static HttpRequestMessage AuthorizedGet(
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static List<string> SplitCsvLines(string csv)
            => csv
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        private static List<string> ReadInlineStringsFromXlsx(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(sheet);
            using var reader = new StreamReader(sheet!.Open());
            var xml = reader.ReadToEnd();
            var doc = XDocument.Parse(xml);
            XNamespace ns =
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            return doc
                .Descendants(ns + "t")
                .Select(t => t.Value)
                .ToList();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int QrCodeId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Export Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var qr = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Status = QrCodeStatus.Active,
                Token = $"tok-{emailLocalPart}-{Guid.NewGuid():N}"[..40],
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, qr.Id);
        }

        private async Task SeedManyFeedbackAsync(
            int locationId,
            int count,
            DateTime baseCreatedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var qrCodeId = await context.QrCodes
                .Where(q => q.RestaurantLocationId == locationId)
                .Select(q => q.Id)
                .FirstAsync();

            const int batchSize = 500;
            for (var offset = 0; offset < count; offset += batchSize)
            {
                var batch = Math.Min(batchSize, count - offset);
                for (var i = 0; i < batch; i++)
                {
                    var index = offset + i;
                    context.Feedbacks.Add(new Feedback
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qrCodeId,
                        GuestName = $"Guest {index}",
                        GuestContact = $"g{index}@example.com",
                        ContactType = ContactType.Email,
                        Comment = $"Comment {index}",
                        CreatedAt = baseCreatedAt.AddMinutes(index),
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Neutral,
                        DetectedTagsJson = "[]",
                        WorkflowStatus = FeedbackWorkflowStatus.New,
                    });
                }

                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
        }

        private async Task<int> AddFeedbackAsync(
            int locationId,
            DateTime createdAt,
            string comment,
            string guestName,
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            FeedbackWorkflowStatus workflowStatus,
            string? detectedTagsJson = null,
            ContactType contactType = ContactType.Email,
            string guestContact = "alex@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var qrCodeId = await context.QrCodes
                .Where(q => q.RestaurantLocationId == locationId)
                .Select(q => q.Id)
                .FirstAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = locationId,
                QrCodeId = qrCodeId,
                GuestName = guestName,
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = comment,
                CreatedAt = createdAt,
                ClassificationStatus = classificationStatus,
                Sentiment = sentiment,
                DetectedTagsJson =
                    detectedTagsJson
                    ?? (classificationStatus == ClassificationStatus.Succeeded
                        ? "[]"
                        : null),
                WorkflowStatus = workflowStatus,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();
            return feedback.Id;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
