using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class SupportListQueriesTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly SupportService _service;

        public SupportListQueriesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();

            _service = new SupportService(
                _context,
                new EmailServiceStubBase(),
                new StubQueryAttachmentStorage(),
                Options.Create(new HelpCentreSettings()),
                configuration,
                NullLogger<SupportService>.Instance
            );
        }

        [Fact]
        public async Task ListQueriesAsync_Paginates_AndReturnsTotalCount()
        {
            await SeedQueriesAsync(25);

            var result = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: null,
                type: null,
                page: 2,
                pageSize: 20
            );

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(25, doc.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(5, doc.RootElement.GetProperty("queries").GetArrayLength());
        }

        [Fact]
        public async Task ListQueriesAsync_FiltersByType_Operator()
        {
            await SeedQueriesAsync(3, operatorCount: 2);

            var result = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: null,
                type: "operator",
                page: 1,
                pageSize: 20
            );

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
            Assert.All(
                doc.RootElement.GetProperty("queries").EnumerateArray(),
                item => Assert.True(item.GetProperty("linkedOperator").GetBoolean())
            );
        }

        [Fact]
        public async Task ListQueriesAsync_FiltersByType_Contact()
        {
            await SeedQueriesAsync(3, operatorCount: 1);

            var result = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: null,
                type: "contact",
                page: 1,
                pageSize: 20
            );

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(2, doc.RootElement.GetProperty("totalCount").GetInt32());
            Assert.All(
                doc.RootElement.GetProperty("queries").EnumerateArray(),
                item => Assert.False(item.GetProperty("linkedOperator").GetBoolean())
            );
        }

        [Fact]
        public async Task ListQueriesAsync_SearchesSubmitterAndBusiness()
        {
            await SeedQueriesAsync(2);
            _context.HelpCentreQueries.Add(
                new HelpCentreQuery
                {
                    Topic = HelpCentreQueryTopic.Setup,
                    SubmitterName = "Riverside Owner",
                    SubmitterEmail = "owner@riverside.test",
                    BusinessName = "Riverside Bistro",
                    Status = HelpCentreQueryStatus.New,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Messages =
                    [
                        new HelpCentreQueryMessage
                        {
                            AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                            Body = "Need setup help",
                            CreatedAt = DateTime.UtcNow,
                        },
                    ],
                }
            );
            await _context.SaveChangesAsync();

            var result = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: "riverside",
                type: null,
                page: 1,
                pageSize: 20
            );

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Riverside Bistro",
                doc.RootElement.GetProperty("queries")[0]
                    .GetProperty("businessName")
                    .GetString()
            );
        }

        [Fact]
        public async Task ListQueriesAsync_SearchesLatestMessagePreview_NotOlderBodies()
        {
            await SeedQueriesAsync(1);

            var query = new HelpCentreQuery
            {
                Topic = HelpCentreQueryTopic.Setup,
                SubmitterName = "Pat",
                SubmitterEmail = "pat@example.com",
                BusinessName = "Pat Cafe",
                Status = HelpCentreQueryStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                        Body = "UniqueOlderToken should not match alone",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    },
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Support,
                        Body = "Latest preview UniquePreviewToken",
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            };
            _context.HelpCentreQueries.Add(query);
            await _context.SaveChangesAsync();

            var previewHit = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: "UniquePreviewToken",
                type: null,
                page: 1,
                pageSize: 20
            );
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(previewHit)))
            {
                Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
            }

            var olderMiss = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: "UniqueOlderToken",
                type: null,
                page: 1,
                pageSize: 20
            );
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(olderMiss)))
            {
                Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
            }
        }

        [Fact]
        public async Task ListQueriesAsync_DefaultsInvalidPageSizeToTwenty()
        {
            await SeedQueriesAsync(25);

            var result = await _service.ListQueriesAsync(
                status: null,
                topic: null,
                q: null,
                type: null,
                page: 1,
                pageSize: 7
            );

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(20, doc.RootElement.GetProperty("queries").GetArrayLength());
        }

        private async Task SeedQueriesAsync(int count, int operatorCount = 0)
        {
            for (var i = 0; i < count; i++)
            {
                _context.HelpCentreQueries.Add(
                    new HelpCentreQuery
                    {
                        Topic = HelpCentreQueryTopic.Billing,
                        SubmitterName = $"Person {i}",
                        SubmitterEmail = $"person{i}@example.com",
                        BusinessName = $"Business {i}",
                        Status = HelpCentreQueryStatus.New,
                        UserId = i < operatorCount ? 100 + i : null,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-i),
                        Messages =
                        [
                            new HelpCentreQueryMessage
                            {
                                AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                                Body = $"Message {i}",
                                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                            },
                        ],
                    }
                );
            }

            await _context.SaveChangesAsync();
        }

        public void Dispose() => _context.Dispose();

        private sealed class StubQueryAttachmentStorage : IQueryAttachmentStorage
        {
            public bool IsConfigured => false;

            public Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<Stream>(Stream.Null);

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
