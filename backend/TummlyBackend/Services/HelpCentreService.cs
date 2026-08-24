using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class HelpCentreService : IHelpCentreService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocationService;
        private readonly IEmailService _emailService;
        private readonly IQueryAttachmentStorage _attachmentStorage;
        private readonly HelpCentreSettings _settings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HelpCentreService> _logger;

        public HelpCentreService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocationService,
            IEmailService emailService,
            IQueryAttachmentStorage attachmentStorage,
            IOptions<HelpCentreSettings> settings,
            IConfiguration configuration,
            ILogger<HelpCentreService> logger
        )
        {
            _context = context;
            _ownedLocationService = ownedLocationService;
            _emailService = emailService;
            _attachmentStorage = attachmentStorage;
            _settings = settings.Value;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<object> CreateQueryAsync(
            CreateHelpCentreQueryDto dto,
            int? userId,
            IReadOnlyList<IFormFile>? attachments = null
        )
        {
            HelpCentreAccountRequestKind? accountRequestKind = null;

            if (!string.IsNullOrWhiteSpace(dto.AccountRequestKind))
            {
                accountRequestKind =
                    HelpCentreAccountRequestKindExtensions.FromWireString(
                        dto.AccountRequestKind
                    );
            }

            if (accountRequestKind.HasValue)
            {
                return await CreateAccountRequestQueryAsync(
                    dto,
                    userId,
                    accountRequestKind.Value
                );
            }

            var attachmentFiles = attachments?
                .Where(file => file.Length > 0)
                .ToList();

            if (attachmentFiles is { Count: > 0 })
            {
                if (!userId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Attachments are only available for signed-in operators."
                    );
                }

                var validationError =
                    HelpCentreAttachmentValidator.ValidateFiles(attachmentFiles);

                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }

                if (!_attachmentStorage.IsConfigured)
                {
                    throw new InvalidOperationException(
                        "File attachments are temporarily unavailable. "
                            + "Please submit without attachments or try again later."
                    );
                }
            }

            var topic = HelpCentreQueryTopicExtensions.FromSlug(dto.Topic);
            var email = dto.SubmitterEmail.Trim().ToLower();

            int? locationId = null;
            string? locationLabel = null;

            if (dto.RestaurantLocationId.HasValue)
            {
                if (!userId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Location can only be set for signed-in operators."
                    );
                }

                var ownership = await _ownedLocationService.ResolveAsync(
                    userId.Value,
                    dto.RestaurantLocationId.Value
                );

                if (ownership.Status != DTOs.OwnedLocation.OwnedLocationResolveStatus.Found)
                {
                    throw new InvalidOperationException(
                        "Selected location is not owned by this account."
                    );
                }

                locationId = dto.RestaurantLocationId.Value;
                locationLabel = ownership.Location?.LocationName;
            }

            var query = new HelpCentreQuery
            {
                Topic = topic,
                SubmitterName = dto.SubmitterName.Trim(),
                SubmitterEmail = email,
                Phone = string.IsNullOrWhiteSpace(dto.Phone)
                    ? null
                    : dto.Phone.Trim(),
                BusinessName = dto.BusinessName.Trim(),
                UserId = userId,
                RestaurantLocationId = locationId,
                Status = HelpCentreQueryStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                        AuthorUserId = userId,
                        Body = dto.Message.Trim(),
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            };

            _context.HelpCentreQueries.Add(query);
            await _context.SaveChangesAsync();

            var uploadedKeys = new List<string>();

            try
            {
                if (attachmentFiles is { Count: > 0 })
                {
                    // Upload to Spaces in parallel (DbContext stays single-threaded).
                    var planned = attachmentFiles
                        .Select(file =>
                        {
                            var storageKey =
                                HelpCentreAttachmentValidator.BuildStorageKey(
                                    query.Id,
                                    file.FileName
                                );
                            var contentType =
                                HelpCentreAttachmentValidator.ResolveContentType(
                                    file.ContentType,
                                    file.FileName
                                )
                                ?? file.ContentType;

                            return (File: file, StorageKey: storageKey, ContentType: contentType);
                        })
                        .ToList();

                    var uploadedKeyBag = new ConcurrentBag<string>();

                    await Task.WhenAll(
                        planned.Select(async item =>
                        {
                            await using var stream = item.File.OpenReadStream();
                            await _attachmentStorage.UploadAsync(
                                item.StorageKey,
                                stream,
                                item.ContentType,
                                item.File.Length,
                                CancellationToken.None
                            );
                            uploadedKeyBag.Add(item.StorageKey);
                        })
                    );

                    uploadedKeys.AddRange(uploadedKeyBag);

                    foreach (var item in planned)
                    {
                        _context.HelpCentreQueryAttachments.Add(
                            new HelpCentreQueryAttachment
                            {
                                QueryId = query.Id,
                                OriginalFileName = Path.GetFileName(item.File.FileName),
                                ContentType = item.ContentType,
                                SizeBytes = item.File.Length,
                                StorageKey = item.StorageKey,
                                CreatedAt = DateTime.UtcNow,
                            }
                        );
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to store attachments for query {QueryId}: {ErrorMessage}",
                    query.Id,
                    ex.Message
                );

                foreach (var storageKey in uploadedKeys)
                {
                    try
                    {
                        await _attachmentStorage.DeleteAsync(storageKey);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(
                            deleteEx,
                            "Failed to delete attachment blob {StorageKey}",
                            storageKey
                        );
                    }
                }

                _context.HelpCentreQueryAttachments.RemoveRange(
                    _context.HelpCentreQueryAttachments.Where(
                        attachment => attachment.QueryId == query.Id
                    )
                );
                _context.HelpCentreQueries.Remove(query);
                await _context.SaveChangesAsync();

                throw new InvalidOperationException(
                    "Unable to upload attachments. Please try again."
                );
            }

            var emailDispatched = await EmailDispatch.TrySendAsync(
                () => _emailService.SendHelpCentreNewQueryEmailAsync(
                    query.Topic.ToDisplayLabel(),
                    query.SubmitterName,
                    query.SubmitterEmail,
                    query.BusinessName,
                    locationLabel,
                    dto.Message.Trim(),
                    attachmentFiles?.Count ?? 0,
                    BuildSupportDashboardUrl()
                ),
                _logger,
                "Failed to send new query email for query {QueryId}",
                query.Id
            );

            return new
            {
                id = query.Id,
                status = query.Status.ToWireString(),
                emailDispatched,
                emailWarning = EmailDispatch.WarningOrNull(emailDispatched),
            };
        }

        public async Task<object?> GetOpenAccountRequestAsync(
            int userId,
            int restaurantId,
            string accountRequestKind
        )
        {
            if (
                !HelpCentreAccountRequestKindExtensions.TryParseWireString(
                    accountRequestKind,
                    out var kind
                )
            )
            {
                throw new InvalidOperationException(
                    "Invalid account request kind."
                );
            }

            await EnsureAccountRequestOwnerAsync(userId, restaurantId);

            var existingQueryId = await FindOpenAccountRequestQueryIdAsync(
                restaurantId,
                kind
            );

            if (existingQueryId == null)
            {
                return new { queryId = (int?)null };
            }

            return new { queryId = existingQueryId };
        }

        private async Task<object> CreateAccountRequestQueryAsync(
            CreateHelpCentreQueryDto dto,
            int? userId,
            HelpCentreAccountRequestKind accountRequestKind
        )
        {
            if (!userId.HasValue)
            {
                throw new InvalidOperationException(
                    "Account requests require a signed-in operator."
                );
            }

            if (!dto.RestaurantId.HasValue)
            {
                throw new InvalidOperationException(
                    "Restaurant id is required for account requests."
                );
            }

            if (dto.RestaurantLocationId.HasValue)
            {
                throw new InvalidOperationException(
                    "Query location must be unset for account requests."
                );
            }

            var restaurant = await EnsureAccountRequestOwnerAsync(
                userId.Value,
                dto.RestaurantId.Value
            );

            var existingQueryId = await FindOpenAccountRequestQueryIdAsync(
                restaurant.Id,
                accountRequestKind
            );

            if (existingQueryId != null)
            {
                throw new DuplicateOpenAccountRequestException(
                    existingQueryId.Value
                );
            }

            var topic =
                HelpCentreAccountRequestKindExtensions.TopicForKind(
                    accountRequestKind
                );
            var email = dto.SubmitterEmail.Trim().ToLower();
            var message = BuildAccountRequestMessage(
                accountRequestKind,
                restaurant.Name,
                restaurant.Id,
                email
            );

            var query = new HelpCentreQuery
            {
                Topic = topic,
                SubmitterName = dto.SubmitterName.Trim(),
                SubmitterEmail = email,
                Phone = string.IsNullOrWhiteSpace(dto.Phone)
                    ? null
                    : dto.Phone.Trim(),
                BusinessName = dto.BusinessName.Trim(),
                UserId = userId,
                RestaurantLocationId = null,
                AccountRequestKind = accountRequestKind,
                RestaurantId = restaurant.Id,
                Status = HelpCentreQueryStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                        AuthorUserId = userId,
                        Body = message,
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            };

            _context.HelpCentreQueries.Add(query);
            await _context.SaveChangesAsync();

            var emailDispatched = await EmailDispatch.TrySendAsync(
                () => _emailService.SendHelpCentreNewQueryEmailAsync(
                    query.Topic.ToDisplayLabel(),
                    query.SubmitterName,
                    query.SubmitterEmail,
                    query.BusinessName,
                    locationLabel: null,
                    message,
                    attachmentCount: 0,
                    BuildSupportDashboardUrl()
                ),
                _logger,
                "Failed to send new query email for query {QueryId}",
                query.Id
            );

            return new
            {
                id = query.Id,
                status = query.Status.ToWireString(),
                emailDispatched,
                emailWarning = EmailDispatch.WarningOrNull(emailDispatched),
            };
        }

        private async Task<Restaurant> EnsureAccountRequestOwnerAsync(
            int userId,
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
            {
                throw new InvalidOperationException(
                    "Restaurant not found."
                );
            }

            if (restaurant.OwnerUserId != userId)
            {
                throw new InvalidOperationException(
                    "Only the account owner can submit account requests."
                );
            }

            return restaurant;
        }

        private async Task<int?> FindOpenAccountRequestQueryIdAsync(
            int restaurantId,
            HelpCentreAccountRequestKind accountRequestKind
        )
        {
            return await _context.HelpCentreQueries
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantId == restaurantId
                    && q.AccountRequestKind == accountRequestKind
                    && (
                        q.Status == HelpCentreQueryStatus.New
                        || q.Status == HelpCentreQueryStatus.InProgress
                        || q.Status
                            == HelpCentreQueryStatus.WaitingOnCustomer
                        || q.Status
                            == HelpCentreQueryStatus.EscalatedToAdmin
                    )
                )
                .OrderByDescending(q => q.UpdatedAt)
                .Select(q => (int?)q.Id)
                .FirstOrDefaultAsync();
        }

        private static string BuildAccountRequestMessage(
            HelpCentreAccountRequestKind accountRequestKind,
            string workspaceName,
            int restaurantId,
            string actorEmail
        )
        {
            var closingLine = accountRequestKind switch
            {
                HelpCentreAccountRequestKind.TransferOwnership =>
                    "Ownership transfer requested from Account controls.",
                HelpCentreAccountRequestKind.AccountExport =>
                    "Account export requested from Account controls.",
                HelpCentreAccountRequestKind.AccountClosure =>
                    "Account closure requested from Account controls.",
                _ => "Account request submitted from Account controls.",
            };

            return string.Join(
                "\n",
                $"Account request kind: {accountRequestKind.ToDisplayLabel()}",
                $"Workspace name: {workspaceName}",
                $"Restaurant id: {restaurantId}",
                $"Actor email: {actorEmail}",
                closingLine
            );
        }

        public async Task<object> ListMyQueriesAsync(int userId)
        {
            var queries = await _context.HelpCentreQueries
                .AsNoTracking()
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.UpdatedAt)
                .Select(q => new
                {
                    q.Id,
                    topic = q.Topic.ToSlug(),
                    topicLabel = q.Topic.ToDisplayLabel(),
                    status = q.Status.ToWireString(),
                    statusLabel = q.Status.ToDisplayLabel(),
                    q.UpdatedAt,
                    preview = q.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Body)
                        .FirstOrDefault(),
                })
                .ToListAsync();

            return new { queries };
        }

        public async Task<object?> GetMyQueryAsync(int userId, int queryId)
        {
            var query = await _context.HelpCentreQueries
                .AsNoTracking()
                .Include(q => q.RestaurantLocation)
                .Include(q => q.Messages.OrderBy(m => m.CreatedAt))
                .Include(q => q.Attachments.OrderBy(a => a.CreatedAt))
                .FirstOrDefaultAsync(q => q.Id == queryId && q.UserId == userId);

            if (query == null)
            {
                return null;
            }

            return MapQueryDetail(query);
        }

        public async Task<object> AddOperatorReplyAsync(
            int userId,
            int queryId,
            OperatorReplyDto dto
        )
        {
            var query = await _context.HelpCentreQueries
                .Include(q => q.Messages)
                .Include(q => q.Attachments)
                .Include(q => q.RestaurantLocation)
                .FirstOrDefaultAsync(q => q.Id == queryId && q.UserId == userId);

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
                AuthorKind = HelpCentreQueryAuthorKind.Operator,
                AuthorUserId = userId,
                Body = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            query.Messages.Add(message);
            query.UpdatedAt = DateTime.UtcNow;

            if (query.Status == HelpCentreQueryStatus.WaitingOnCustomer)
            {
                query.Status = HelpCentreQueryStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            var emailDispatched = await EmailDispatch.TrySendAsync(
                () => _emailService.SendHelpCentreOperatorReplyEmailAsync(
                    query.Topic.ToDisplayLabel(),
                    query.SubmitterName,
                    query.SubmitterEmail,
                    query.BusinessName,
                    message.Body,
                    BuildSupportDashboardUrl()
                ),
                _logger,
                "Failed to send operator reply email for query {QueryId}",
                query.Id
            );

            return MapQueryDetail(query, emailDispatched);
        }

        public async Task<object?> GetContactPrefillAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == userId);

            var locations = restaurant == null
                ? []
                : await _context.RestaurantLocations
                    .AsNoTracking()
                    .Where(l => l.RestaurantId == restaurant.Id)
                    .OrderBy(l => l.CreatedAt)
                    .Select(l => new
                    {
                        id = l.Id,
                        label = l.LocationName,
                    })
                    .ToListAsync();

            return new
            {
                businessName = restaurant?.Name ?? string.Empty,
                submitterName = user.FullName,
                submitterEmail = user.Email,
                locations,
            };
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?>
            GetMyQueryAttachmentAsync(
                int userId,
                int queryId,
                int attachmentId
            )
        {
            var attachment = await _context.HelpCentreQueryAttachments
                .AsNoTracking()
                .Include(a => a.Query)
                .FirstOrDefaultAsync(
                    a =>
                        a.Id == attachmentId
                        && a.QueryId == queryId
                        && a.Query.UserId == userId
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
                canReply = query.Status is HelpCentreQueryStatus.New
                    or HelpCentreQueryStatus.InProgress
                    or HelpCentreQueryStatus.WaitingOnCustomer,
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

        private string BuildSupportDashboardUrl()
        {
            var baseUrl = GetFrontendBaseUrl();
            return $"{baseUrl}/support-dashboard";
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
