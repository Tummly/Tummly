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
    public class SupportEmailDispatchTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingSupportEmailService _emailService;
        private readonly SupportService _service;

        public SupportEmailDispatchTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingSupportEmailService();

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
                _emailService,
                new StubQueryAttachmentStorage(),
                Options.Create(new HelpCentreSettings()),
                configuration,
                NullLogger<SupportService>.Instance
            );
        }

        [Fact]
        public async Task AddSupportReplyAsync_StillPersists_WhenEmailFails()
        {
            var query = await SeedQueryAsync();
            _emailService.ThrowOnSupportReply = true;

            var result = await _service.AddSupportReplyAsync(
                staffId: 9,
                query.Id,
                new SupportReplyDto { Body = "We are looking into this." }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("emailDispatched").ValueKind
            );
            Assert.Equal(
                2,
                await _context.HelpCentreQueryMessages.CountAsync(
                    m => m.QueryId == query.Id
                )
            );
        }

        [Fact]
        public async Task UpdateStatusAsync_StillPersistsEscalation_WhenEmailFails()
        {
            var query = await SeedQueryAsync();
            _emailService.ThrowOnEscalation = true;

            var result = await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto
                {
                    Status = "ESCALATED_TO_ADMIN",
                    EscalationNote = "Needs product input",
                }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("emailDispatched").ValueKind
            );

            var saved = await _context.HelpCentreQueries.SingleAsync(
                q => q.Id == query.Id
            );
            Assert.Equal(HelpCentreQueryStatus.EscalatedToAdmin, saved.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_OmitsFailedDispatch_WhenNoEmailSent()
        {
            var query = await SeedQueryAsync();

            var result = await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "IN_PROGRESS" }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("emailDispatched").ValueKind
            );
            Assert.Equal(0, _emailService.EscalationEmailCalls);
            Assert.Equal(0, _emailService.ResolvedEmailCalls);
        }

        [Fact]
        public async Task UpdateStatusAsync_SendsResolvedEmail_WithMyQueriesUrl_ForOperator()
        {
            var query = await SeedQueryAsync(userId: 42);
            query.Status = HelpCentreQueryStatus.InProgress;
            await _context.SaveChangesAsync();

            var result = await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "RESOLVED" }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.True(doc.RootElement.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(1, _emailService.ResolvedEmailCalls);
            Assert.Equal(
                $"https://app.tummly.test/help-center/my-queries/{query.Id}",
                _emailService.LastResolvedMyQueriesUrl
            );
            Assert.NotNull(_emailService.LastResolvedExcerpt);
            Assert.Single(_emailService.LastResolvedExcerpt!);
            Assert.Equal("Submitter", _emailService.LastResolvedExcerpt![0].AuthorLabel);
        }

        [Fact]
        public async Task UpdateStatusAsync_SendsResolvedEmail_WithoutMyQueriesUrl_ForContact()
        {
            var query = await SeedQueryAsync();
            query.Status = HelpCentreQueryStatus.InProgress;
            await _context.SaveChangesAsync();

            await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "RESOLVED" }
            );

            Assert.Equal(1, _emailService.ResolvedEmailCalls);
            Assert.Null(_emailService.LastResolvedMyQueriesUrl);
        }

        [Fact]
        public async Task UpdateStatusAsync_DoesNotResendResolvedEmail_WhenAlreadyResolved()
        {
            var query = await SeedQueryAsync();
            query.Status = HelpCentreQueryStatus.Resolved;
            await _context.SaveChangesAsync();

            var result = await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "RESOLVED" }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(
                JsonValueKind.Null,
                doc.RootElement.GetProperty("emailDispatched").ValueKind
            );
            Assert.Equal(0, _emailService.ResolvedEmailCalls);
        }

        [Fact]
        public async Task UpdateStatusAsync_DoesNotSendResolvedEmail_WhenClosing()
        {
            var query = await SeedQueryAsync();
            query.Status = HelpCentreQueryStatus.InProgress;
            await _context.SaveChangesAsync();

            await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "CLOSED" }
            );

            Assert.Equal(0, _emailService.ResolvedEmailCalls);
        }

        [Fact]
        public async Task UpdateStatusAsync_ReturnsEmailDispatchedFalse_WhenResolvedEmailFails()
        {
            var query = await SeedQueryAsync();
            query.Status = HelpCentreQueryStatus.InProgress;
            await _context.SaveChangesAsync();
            _emailService.ThrowOnResolved = true;

            var result = await _service.UpdateStatusAsync(
                staffId: 9,
                query.Id,
                new UpdateQueryStatusDto { Status = "RESOLVED" }
            );

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);

            Assert.False(doc.RootElement.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(
                EmailDispatch.DefaultWarning,
                doc.RootElement.GetProperty("emailWarning").GetString()
            );

            var saved = await _context.HelpCentreQueries.SingleAsync(
                q => q.Id == query.Id
            );
            Assert.Equal(HelpCentreQueryStatus.Resolved, saved.Status);
        }

        private async Task<HelpCentreQuery> SeedQueryAsync(int? userId = null)
        {
            var query = new HelpCentreQuery
            {
                Topic = HelpCentreQueryTopic.Billing,
                SubmitterName = "Jane",
                SubmitterEmail = "jane@example.com",
                BusinessName = "Test Cafe",
                Status = HelpCentreQueryStatus.New,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
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

        private sealed class TrackingSupportEmailService : EmailServiceStubBase
        {
            public int EscalationEmailCalls { get; private set; }

            public int ResolvedEmailCalls { get; private set; }

            public string? LastResolvedMyQueriesUrl { get; private set; }

            public IReadOnlyList<(string AuthorLabel, string Body)>? LastResolvedExcerpt
            {
                get;
                private set;
            }

            public bool ThrowOnSupportReply { get; set; }

            public bool ThrowOnEscalation { get; set; }

            public bool ThrowOnResolved { get; set; }

            public override Task SendHelpCentreSupportReplyEmailAsync(
                string toEmail,
                string submitterName,
                string topicLabel,
                string replyBody,
                string? myQueriesUrl
            )
            {
                if (ThrowOnSupportReply)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }

            public override Task SendHelpCentreResolvedEmailAsync(
                string toEmail,
                string submitterName,
                string topicLabel,
                IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
                string? myQueriesUrl
            )
            {
                ResolvedEmailCalls++;
                LastResolvedMyQueriesUrl = myQueriesUrl;
                LastResolvedExcerpt = excerptMessages;

                if (ThrowOnResolved)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }

            public override Task SendHelpCentreEscalationEmailAsync(
                string toEmail,
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string? locationLabel,
                string threadSummary,
                string? escalationNote,
                string supportDashboardUrl
            )
            {
                EscalationEmailCalls++;

                if (ThrowOnEscalation)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}
