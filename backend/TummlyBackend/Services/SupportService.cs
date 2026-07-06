using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SupportService : ISupportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IQueryAttachmentStorage _attachmentStorage;
        private readonly HelpCentreSettings _settings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupportService> _logger;

        public SupportService(
            ApplicationDbContext context,
            IEmailService emailService,
            IQueryAttachmentStorage attachmentStorage,
            IOptions<HelpCentreSettings> settings,
            IConfiguration configuration,
            ILogger<SupportService> logger
        )
        {
            _context = context;
            _emailService = emailService;
            _attachmentStorage = attachmentStorage;
            _settings = settings.Value;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<object> ListQueriesAsync(
            string? status,
            string? topic
        )
        {
            var query = _context.HelpCentreQueries
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (
                    !HelpCentreQueryStatusExtensions.TryParseWireString(
                        status,
                        out var parsedStatus
                    )
                )
                {
                    throw new ArgumentException("Invalid query status.");
                }

                query = query.Where(q => q.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                if (
                    !HelpCentreQueryTopicExtensions.TryFromSlug(
                        topic,
                        out var parsedTopic
                    )
                )
                {
                    throw new ArgumentException("Invalid query topic.");
                }

                query = query.Where(q => q.Topic == parsedTopic);
            }

            var queries = await query
                .OrderByDescending(q => q.UpdatedAt)
                .Select(q => new
                {
                    q.Id,
                    topic = q.Topic.ToSlug(),
                    topicLabel = q.Topic.ToDisplayLabel(),
                    status = q.Status.ToWireString(),
                    statusLabel = q.Status.ToDisplayLabel(),
                    submitterName = q.SubmitterName,
                    submitterEmail = q.SubmitterEmail,
                    businessName = q.BusinessName,
                    queryLocationLabel = q.RestaurantLocation != null
                        ? q.RestaurantLocation.LocationName
                        : null,
                    linkedOperator = q.UserId != null,
                    linkedOperatorEmail = q.User != null
                        ? q.User.Email
                        : null,
                    preview = q.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Body)
                        .FirstOrDefault(),
                    q.UpdatedAt,
                })
                .ToListAsync();

            return new { queries };
        }

        public async Task<object?> GetQueryAsync(int queryId)
        {
            var query = await _context.HelpCentreQueries
                .AsNoTracking()
                .Include(q => q.User)
                .Include(q => q.RestaurantLocation)
                .Include(q => q.Messages.OrderBy(m => m.CreatedAt))
                .Include(q => q.Attachments.OrderBy(a => a.CreatedAt))
                .FirstOrDefaultAsync(q => q.Id == queryId);

            if (query == null)
            {
                return null;
            }

            return MapQueryDetail(query);
        }

        public async Task<object> AddSupportReplyAsync(
            int staffId,
            int queryId,
            SupportReplyDto dto
        )
        {
            var query = await _context.HelpCentreQueries
                .Include(q => q.Messages)
                .FirstOrDefaultAsync(q => q.Id == queryId);

            if (query == null)
            {
                throw new KeyNotFoundException("Query not found.");
            }

            if (
                query.Status is HelpCentreQueryStatus.Resolved
                    or HelpCentreQueryStatus.Closed
            )
            {
                throw new InvalidOperationException(
                    "This query is closed and cannot accept replies."
                );
            }

            var message = new HelpCentreQueryMessage
            {
                QueryId = query.Id,
                AuthorKind = HelpCentreQueryAuthorKind.Support,
                AuthorStaffId = staffId,
                Body = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            query.Messages.Add(message);
            query.UpdatedAt = DateTime.UtcNow;

            if (query.Status == HelpCentreQueryStatus.New)
            {
                query.Status = HelpCentreQueryStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            string? myQueriesUrl = query.UserId.HasValue
                ? $"{GetFrontendBaseUrl()}/help-center/my-queries/{query.Id}"
                : null;

            try
            {
                await _emailService.SendHelpCentreSupportReplyEmailAsync(
                    query.SubmitterEmail,
                    query.SubmitterName,
                    query.Topic.ToDisplayLabel(),
                    message.Body,
                    myQueriesUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send support reply email for query {QueryId}",
                    query.Id
                );
            }

            return MapQueryDetail(query);
        }

        public async Task<object> UpdateStatusAsync(
            int staffId,
            int queryId,
            UpdateQueryStatusDto dto
        )
        {
            var query = await _context.HelpCentreQueries
                .Include(q => q.Messages.OrderBy(m => m.CreatedAt))
                .Include(q => q.RestaurantLocation)
                .FirstOrDefaultAsync(q => q.Id == queryId);

            if (query == null)
            {
                throw new KeyNotFoundException("Query not found.");
            }

            if (
                !HelpCentreQueryStatusExtensions.TryParseWireString(
                    dto.Status,
                    out var newStatus
                )
            )
            {
                throw new ArgumentException("Invalid query status.");
            }

            query.Status = newStatus;
            query.UpdatedAt = DateTime.UtcNow;

            if (newStatus == HelpCentreQueryStatus.EscalatedToAdmin)
            {
                query.EscalationNote = string.IsNullOrWhiteSpace(dto.EscalationNote)
                    ? null
                    : dto.EscalationNote.Trim();
            }

            await _context.SaveChangesAsync();

            if (newStatus == HelpCentreQueryStatus.EscalatedToAdmin)
            {
                var threadSummary = string.Join(
                    "\n\n",
                    query.Messages.Select(m =>
                        $"[{m.AuthorKind.ToWireString()}] {m.Body}"
                    )
                );

                try
                {
                    await _emailService.SendHelpCentreEscalationEmailAsync(
                        _settings.AdminNotificationEmail,
                        query.Topic.ToDisplayLabel(),
                        query.SubmitterName,
                        query.SubmitterEmail,
                        query.BusinessName,
                        query.RestaurantLocation?.LocationName,
                        threadSummary,
                        query.EscalationNote,
                        $"{GetFrontendBaseUrl()}/support-dashboard"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send escalation email for query {QueryId}",
                        query.Id
                    );
                }
            }

            return MapQueryDetail(query);
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?>
            GetQueryAttachmentAsync(int queryId, int attachmentId)
        {
            var attachment = await _context.HelpCentreQueryAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.Id == attachmentId && a.QueryId == queryId
                );

            if (attachment == null)
            {
                return null;
            }

            if (!_attachmentStorage.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Object storage is not configured."
                );
            }

            var stream = await _attachmentStorage.OpenReadAsync(
                attachment.StorageKey
            );

            return (stream, attachment.ContentType, attachment.OriginalFileName);
        }

        private object MapQueryDetail(HelpCentreQuery query)
        {
            return new
            {
                id = query.Id,
                topic = query.Topic.ToSlug(),
                topicLabel = query.Topic.ToDisplayLabel(),
                status = query.Status.ToWireString(),
                statusLabel = query.Status.ToDisplayLabel(),
                submitterName = query.SubmitterName,
                submitterEmail = query.SubmitterEmail,
                phone = query.Phone,
                businessName = query.BusinessName,
                queryLocation = query.RestaurantLocation == null
                    ? null
                    : new
                    {
                        id = query.RestaurantLocation.Id,
                        label = query.RestaurantLocation.LocationName,
                    },
                linkedOperator = query.UserId != null,
                linkedOperatorEmail = query.User?.Email,
                escalationNote = query.EscalationNote,
                createdAt = query.CreatedAt,
                updatedAt = query.UpdatedAt,
                messages = query.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        authorKind = m.AuthorKind.ToWireString(),
                        body = m.Body,
                        createdAt = m.CreatedAt,
                    }),
                attachments = query.Attachments
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new
                    {
                        id = a.Id,
                        fileName = a.OriginalFileName,
                        contentType = a.ContentType,
                        sizeBytes = a.SizeBytes,
                        createdAt = a.CreatedAt,
                    }),
            };
        }

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            return frontendBaseUrl;
        }
    }
}
