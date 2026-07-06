using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.DTOs.HelpCentre;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<HelpCentreService> _logger;

        public HelpCentreService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocationService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<HelpCentreService> logger
        )
        {
            _context = context;
            _ownedLocationService = ownedLocationService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<object> CreateQueryAsync(
            CreateHelpCentreQueryDto dto,
            int? userId
        )
        {
            var topic = HelpCentreQueryTopicExtensions.FromSlug(dto.Topic);
            var email = dto.SubmitterEmail.Trim().ToLower();

            int? locationId = null;

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

            return new
            {
                id = query.Id,
                status = query.Status.ToWireString(),
            };
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

            try
            {
                await _emailService.SendHelpCentreOperatorReplyEmailAsync(
                    query.Topic.ToDisplayLabel(),
                    query.SubmitterName,
                    query.SubmitterEmail,
                    query.BusinessName,
                    message.Body,
                    BuildSupportDashboardUrl()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send operator reply email for query {QueryId}",
                    query.Id
                );
            }

            return MapQueryDetail(query);
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
