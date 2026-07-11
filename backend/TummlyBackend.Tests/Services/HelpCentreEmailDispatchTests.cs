using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class HelpCentreEmailDispatchTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingHelpCentreEmailService _emailService;
        private readonly HelpCentreService _service;

        public HelpCentreEmailDispatchTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingHelpCentreEmailService();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();

            _service = new HelpCentreService(
                _context,
                new StubOwnedLocationService(),
                _emailService,
                new StubQueryAttachmentStorage(),
                Options.Create(new HelpCentreSettings()),
                configuration,
                NullLogger<HelpCentreService>.Instance
            );
        }

        [Fact]
        public async Task CreateQueryAsync_ReturnsEmailDispatchedTrue_WhenEmailSucceeds()
        {
            var result = await _service.CreateQueryAsync(
                CreateDto(),
                userId: null
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.True(doc.RootElement.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("emailWarning").ValueKind
            );
            Assert.Equal(1, _emailService.NewQueryEmailCalls);
        }

        [Fact]
        public async Task CreateQueryAsync_ReturnsEmailDispatchedFalse_WhenEmailFails()
        {
            _emailService.ThrowOnNewQuery = true;

            var result = await _service.CreateQueryAsync(
                CreateDto(),
                userId: null
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.False(doc.RootElement.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(
                EmailDispatch.DefaultWarning,
                doc.RootElement.GetProperty("emailWarning").GetString()
            );
            Assert.Single(await _context.HelpCentreQueries.ToListAsync());
        }

        [Fact]
        public async Task AddOperatorReplyAsync_ReturnsEmailDispatchedFalse_WhenEmailFails()
        {
            var query = await SeedOperatorQueryAsync();
            _emailService.ThrowOnOperatorReply = true;

            var result = await _service.AddOperatorReplyAsync(
                userId: 1,
                query.Id,
                new OperatorReplyDto { Body = "Any update?" }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.False(doc.RootElement.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(
                EmailDispatch.DefaultWarning,
                doc.RootElement.GetProperty("emailWarning").GetString()
            );
            Assert.Equal(
                2,
                await _context.HelpCentreQueryMessages.CountAsync(
                    m => m.QueryId == query.Id
                )
            );
        }

        private static CreateHelpCentreQueryDto CreateDto() =>
            new()
            {
                Topic = "billing",
                BusinessName = "Test Cafe",
                SubmitterName = "Jane",
                SubmitterEmail = "jane@example.com",
                Message = "Need help with credits.",
            };

        private async Task<HelpCentreQuery> SeedOperatorQueryAsync()
        {
            var user = new User
            {
                Id = 1,
                Email = "owner@example.com",
                FullName = "Owner",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);

            var query = new HelpCentreQuery
            {
                Topic = HelpCentreQueryTopic.Billing,
                SubmitterName = "Jane",
                SubmitterEmail = "jane@example.com",
                BusinessName = "Test Cafe",
                UserId = 1,
                Status = HelpCentreQueryStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                        AuthorUserId = 1,
                        Body = "Initial message",
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            };

            _context.HelpCentreQueries.Add(query);
            await _context.SaveChangesAsync();
            return query;
        }

        public void Dispose() => _context.Dispose();

        private sealed class StubOwnedLocationService : IOwnedLocationService
        {
            public Task<DTOs.OwnedLocation.OwnedLocationResult> ResolveAsync(
                int userId,
                int locationId
            ) =>
                throw new NotSupportedException(
                    "Location resolution is not used in these tests."
                );
        }

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

        private sealed class TrackingHelpCentreEmailService : EmailServiceStubBase
        {
            public int NewQueryEmailCalls { get; private set; }

            public bool ThrowOnNewQuery { get; set; }

            public bool ThrowOnOperatorReply { get; set; }

            public override Task SendHelpCentreNewQueryEmailAsync(
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string? locationLabel,
                string messagePreview,
                int attachmentCount,
                string supportDashboardUrl
            )
            {
                NewQueryEmailCalls++;

                if (ThrowOnNewQuery)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }

            public override Task SendHelpCentreOperatorReplyEmailAsync(
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string replyBody,
                string supportDashboardUrl
            )
            {
                if (ThrowOnOperatorReply)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}
