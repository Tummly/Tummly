using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SupportService : ISupportService
    {
        private const int ExcerptMessageLimit = 3;
        private const int ExcerptBodyMaxLength = 280;

        private static readonly int[] AllowedPageSizes = [20, 50, 100];

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
            string? topic,
            string? q,
            string? type,
            int page = 1,
            int pageSize = 20
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

            if (!string.IsNullOrWhiteSpace(type))
            {
                var normalizedType = type.Trim().ToLowerInvariant();
                query = normalizedType switch
                {
                    "operator" => query.Where(q => q.UserId != null),
                    "contact" => query.Where(q => q.UserId == null),
                    _ => throw new ArgumentException("Invalid query type."),
                };
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                var matchingTopics = Enum.GetValues<HelpCentreQueryTopic>()
                    .Where(t =>
                        t.ToDisplayLabel()
                            .Contains(term, StringComparison.OrdinalIgnoreCase)
                        || t.ToSlug()
                            .Contains(term, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                // Match prior client search: name/email/business/topic + latest-message preview
                query = query.Where(item =>
                    item.SubmitterName.ToLower().Contains(term)
                    || item.SubmitterEmail.ToLower().Contains(term)
                    || item.BusinessName.ToLower().Contains(term)
                    || matchingTopics.Contains(item.Topic)
                    || (
                        item.Messages
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.Body)
                            .FirstOrDefault() ?? string.Empty
                    )
                        .ToLower()
                        .Contains(term)
                );
            }

            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = AllowedPageSizes.Contains(pageSize)
                ? pageSize
                : 20;

            var totalCount = await query.CountAsync();

            var queries = await query
                .OrderByDescending(item => item.UpdatedAt)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(item => new
                {
                    item.Id,
                    topic = item.Topic.ToSlug(),
                    topicLabel = item.Topic.ToDisplayLabel(),
                    status = item.Status.ToWireString(),
                    statusLabel = item.Status.ToDisplayLabel(),
                    submitterName = item.SubmitterName,
                    submitterEmail = item.SubmitterEmail,
                    businessName = item.BusinessName,
                    queryLocationLabel = item.RestaurantLocation != null
                        ? item.RestaurantLocation.LocationName
                        : null,
                    linkedOperator = item.UserId != null,
                    linkedOperatorEmail = item.User != null
                        ? item.User.Email
                        : null,
                    preview = item.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Body)
                        .FirstOrDefault(),
                    item.UpdatedAt,
                })
                .ToListAsync();

            return new { queries, totalCount };
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

            // Soft-fail reply email (unchanged behaviour); do not surface dispatch meta
            _ = await EmailDispatch.TrySendAsync(
                () => _emailService.SendHelpCentreSupportReplyEmailAsync(
                    query.SubmitterEmail,
                    query.SubmitterName,
                    query.Topic.ToDisplayLabel(),
                    message.Body,
                    BuildMyQueriesUrl(query)
                ),
                _logger,
                "Failed to send support reply email for query {QueryId}",
                query.Id
            );

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

            var previousStatus = query.Status;

            query.Status = newStatus;
            query.UpdatedAt = DateTime.UtcNow;

            if (newStatus == HelpCentreQueryStatus.EscalatedToAdmin)
            {
                query.EscalationNote = string.IsNullOrWhiteSpace(dto.EscalationNote)
                    ? null
                    : dto.EscalationNote.Trim();
            }

            await _context.SaveChangesAsync();

            bool? emailDispatched = null;

            if (newStatus == HelpCentreQueryStatus.EscalatedToAdmin)
            {
                var threadSummary = string.Join(
                    "\n\n",
                    query.Messages.Select(m =>
                        $"[{m.AuthorKind.ToWireString()}] {m.Body}"
                    )
                );

                // Soft-fail escalation email (unchanged behaviour); no dispatch meta
                _ = await EmailDispatch.TrySendAsync(
                    () => _emailService.SendHelpCentreEscalationEmailAsync(
                        _settings.AdminNotificationEmail,
                        query.Topic.ToDisplayLabel(),
                        query.SubmitterName,
                        query.SubmitterEmail,
                        query.BusinessName,
                        query.RestaurantLocation?.LocationName,
                        threadSummary,
                        query.EscalationNote,
                        $"{GetFrontendBaseUrl()}/support-dashboard"
                    ),
                    _logger,
                    "Failed to send escalation email for query {QueryId}",
                    query.Id
                );
            }
            else if (
                previousStatus != HelpCentreQueryStatus.Resolved
                && newStatus == HelpCentreQueryStatus.Resolved
            )
            {
                var excerptMessages = BuildResolvedExcerpt(query.Messages);

                emailDispatched = await EmailDispatch.TrySendAsync(
                    () => _emailService.SendHelpCentreResolvedEmailAsync(
                        query.SubmitterEmail,
                        query.SubmitterName,
                        query.Topic.ToDisplayLabel(),
                        excerptMessages,
                        BuildMyQueriesUrl(query)
                    ),
                    _logger,
                    "Failed to send resolution email for query {QueryId}",
                    query.Id
                );
            }

            return MapQueryDetail(query, emailDispatched);
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

        private string? BuildMyQueriesUrl(HelpCentreQuery query) =>
            query.UserId.HasValue
                ? $"{GetFrontendBaseUrl()}/help-center/my-queries/{query.Id}"
                : null;

        private static IReadOnlyList<(string AuthorLabel, string Body)>
            BuildResolvedExcerpt(IEnumerable<HelpCentreQueryMessage> messages)
        {
            return messages
                .OrderByDescending(m => m.CreatedAt)
                .Take(ExcerptMessageLimit)
                .OrderBy(m => m.CreatedAt)
                .Select(m => (
                    ToAuthorLabel(m.AuthorKind),
                    TruncateForExcerpt(m.Body)
                ))
                .ToList();
        }

        private static string ToAuthorLabel(HelpCentreQueryAuthorKind kind) =>
            kind switch
            {
                HelpCentreQueryAuthorKind.Support => "Support",
                HelpCentreQueryAuthorKind.Operator => "Operator",
                _ => "Submitter",
            };

        private static string TruncateForExcerpt(string body)
        {
            var trimmed = body.Trim();
            if (trimmed.Length <= ExcerptBodyMaxLength)
            {
                return trimmed;
            }

            return trimmed[..ExcerptBodyMaxLength].TrimEnd() + "…";
        }

        private object MapQueryDetail(
            HelpCentreQuery query,
            bool? emailDispatched = null
        )
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
                emailDispatched,
                emailWarning = emailDispatched is false
                    ? EmailDispatch.DefaultWarning
                    : null,
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
