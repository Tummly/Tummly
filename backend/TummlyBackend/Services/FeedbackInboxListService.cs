using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackInboxListService : IFeedbackInboxListService
    {
        public const int DefaultPageSize = 25;

        private static readonly HashSet<string> AllowedSorts = new(
            StringComparer.Ordinal
        )
        {
            "newest-submitted",
            "oldest-submitted",
            "needs-attention-first",
            "oldest-unresolved",
            "recently-updated",
            "negative-first",
            "positive-first",
            "guest-name-az",
        };

        private static readonly HashSet<string> AllowedTabs = new(
            StringComparer.Ordinal
        )
        {
            "all",
            "needs-attention",
            "new",
            "in-progress",
            "resolved",
        };

        private static readonly Dictionary<string, QrType> CatalogQrSources =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["CounterCard"] = QrType.CounterCard,
                ["PackagingSticker"] = QrType.PackagingSticker,
                ["DeliveryInsert"] = QrType.DeliveryInsert,
                ["WindowSticker"] = QrType.WindowSticker,
                ["SmartGuest"] = QrType.SmartGuest,
            };

        private static readonly (string Key, string Label)[] DetectedTagLabels =
        [
            (nameof(DetectedTag.FoodQuality), "Food quality"),
            (nameof(DetectedTag.Service), "Service"),
            (nameof(DetectedTag.WaitTime), "Wait time"),
            (nameof(DetectedTag.Cleanliness), "Cleanliness"),
            (nameof(DetectedTag.Value), "Value"),
            (nameof(DetectedTag.Atmosphere), "Atmosphere"),
            (nameof(DetectedTag.Billing), "Billing"),
            (nameof(DetectedTag.AllergiesDietary), "Allergies & dietary"),
            (nameof(DetectedTag.BookingSeating), "Booking & seating"),
            (nameof(DetectedTag.Other), "Other"),
        ];

        private readonly ApplicationDbContext _context;

        public FeedbackInboxListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FeedbackInboxListResponse> ListAsync(
            FeedbackInboxListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidatePaging(query.Page, query.PageSize);
            var tab = NormalizeTab(query.Tab);
            var sort = NormalizeSort(query.Sort);
            var sentiments = NormalizeSentiments(query.Sentiment);
            var tagKeys = NormalizeDetectedTags(query.DetectedTags);
            var contact = NormalizeContact(query.Contact);
            var workflowStatuses = NormalizeWorkflowStatuses(query.WorkflowStatus);
            var (catalogTypes, digitalLinkIds) = NormalizeQrSources(
                query.QrSource
            );
            var (filterFrom, filterTo) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    query.DatePreset,
                    query.DateFrom,
                    query.DateTo,
                    query.UtcOffsetMinutes
                );

            var rangeQuery = _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == query.LocationId
                    && f.CreatedAt >= query.FromUtc
                    && f.CreatedAt < query.ToUtc
                );

            var tabCounts = await ComputeTabCountsAsync(
                rangeQuery,
                cancellationToken
            );

            var filtered = ApplyTab(rangeQuery, tab);
            filtered = ApplyWorkflowStatusFilter(filtered, workflowStatuses);
            filtered = ApplySearch(filtered, query.Q);
            filtered = ApplySentimentFilter(filtered, sentiments);
            filtered = ApplyDetectedTagFilter(filtered, tagKeys);
            filtered = ApplyContactFilter(filtered, contact);
            filtered = await ApplyQrSourceFilterAsync(
                filtered,
                catalogTypes,
                digitalLinkIds,
                cancellationToken
            );

            if (filterFrom.HasValue && filterTo.HasValue)
            {
                var from = filterFrom.Value;
                var to = filterTo.Value;
                filtered = filtered.Where(f =>
                    f.CreatedAt >= from && f.CreatedAt < to
                );
            }

            var totalCount = await filtered.CountAsync(cancellationToken);
            var orderedIds = sort == "recently-updated"
                ? await PageRecentlyUpdatedAsync(
                    filtered,
                    query.Page,
                    query.PageSize,
                    cancellationToken
                )
                : await OrderAndPageAsync(
                    filtered,
                    sort,
                    query.Page,
                    query.PageSize,
                    cancellationToken
                );

            var items = await MaterializeItemsAsync(
                orderedIds,
                query.LocationName,
                cancellationToken
            );

            var digitalGuestLinks = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == query.LocationId
                    && q.QrType == QrType.DigitalGuestLink
                    && q.Status != QrCodeStatus.Archived
                    && q.LinkName != null
                    && q.LinkName != ""
                )
                .OrderBy(q => q.LinkName)
                .Select(q => new FeedbackInboxDigitalGuestLinkDto
                {
                    Id = q.Id,
                    LinkName = q.LinkName!,
                })
                .ToListAsync(cancellationToken);

            return new FeedbackInboxListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TabCounts = tabCounts,
                DigitalGuestLinks = digitalGuestLinks,
            };
        }

        public const int ExportSoftMaxRows = 10_000;

        public const string ExportSoftMaxMessage =
            "Export exceeds 10,000 rows. Narrow filters and try again.";

        public const string ExportEmptyMessage =
            "No feedback to export for the selected scope.";

        private static readonly string[] ExportBaseHeaders =
        [
            "Feedback ID",
            "Submitted at",
            "Feedback",
            "Guest response",
            "Classification status",
            "Issue tags",
            "Location",
            "Source",
            "Workflow status",
            "Needs attention",
        ];

        private static readonly string[] ExportContactHeaders =
        [
            "Guest",
            "Email",
            "Mobile",
        ];

        public async Task<FeedbackExportResult> ExportAsync(
            FeedbackExportQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var scope = NormalizeExportScope(query.Scope);
            var format = NormalizeExportFormat(query.Format);
            var sort = scope == "all-in-period"
                ? "newest-submitted"
                : NormalizeSort(query.Sort);

            var filtered = await BuildExportFilteredQueryAsync(
                query,
                scope,
                cancellationToken
            );

            var totalCount = await filtered.CountAsync(cancellationToken);
            if (totalCount == 0)
            {
                throw new ArgumentException(ExportEmptyMessage);
            }

            if (totalCount > ExportSoftMaxRows)
            {
                throw new ArgumentException(ExportSoftMaxMessage);
            }

            var orderedIds = sort == "recently-updated"
                ? await OrderRecentlyUpdatedAllAsync(
                    filtered,
                    cancellationToken
                )
                : await OrderAllAsync(filtered, sort, cancellationToken);

            if (orderedIds.Count > ExportSoftMaxRows)
            {
                throw new ArgumentException(ExportSoftMaxMessage);
            }

            var rows = await MaterializeExportRowsAsync(
                orderedIds,
                query.LocationName,
                query.IncludeGuestContact,
                cancellationToken
            );

            var headers = query.IncludeGuestContact
                ? ExportBaseHeaders.Concat(ExportContactHeaders).ToArray()
                : ExportBaseHeaders;

            var utcNow = DateTime.UtcNow;
            var stamp = utcNow.ToString("yyyyMMdd-HHmmss");
            var extension = format == "csv" ? "csv" : "xlsx";
            var fileName =
                $"tummly-feedback-{query.LocationId}-{stamp}Z.{extension}";

            if (format == "csv")
            {
                return new FeedbackExportResult
                {
                    FileName = fileName,
                    ContentType = "text/csv",
                    Content = Rfc4180Csv.WriteUtf8(headers, rows),
                };
            }

            return new FeedbackExportResult
            {
                FileName = fileName,
                ContentType = OpenXmlSpreadsheet.ContentType,
                Content = OpenXmlSpreadsheet.Write(headers, rows),
            };
        }

        private async Task<IQueryable<Feedback>> BuildExportFilteredQueryAsync(
            FeedbackExportQuery query,
            string scope,
            CancellationToken cancellationToken
        )
        {
            if (query.FeedbackId.HasValue)
            {
                return _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        f.RestaurantLocationId == query.LocationId
                        && f.Id == query.FeedbackId.Value
                    );
            }

            var rangeQuery = _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == query.LocationId
                    && f.CreatedAt >= query.FromUtc
                    && f.CreatedAt < query.ToUtc
                );

            if (scope == "all-in-period")
            {
                return rangeQuery;
            }

            var tab = NormalizeTab(query.Tab);
            var sentiments = NormalizeSentiments(query.Sentiment);
            var tagKeys = NormalizeDetectedTags(query.DetectedTags);
            var contact = NormalizeContact(query.Contact);
            var workflowStatuses = NormalizeWorkflowStatuses(query.WorkflowStatus);
            var (catalogTypes, digitalLinkIds) = NormalizeQrSources(
                query.QrSource
            );
            var (filterFrom, filterTo) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    query.DatePreset,
                    query.DateFrom,
                    query.DateTo,
                    query.UtcOffsetMinutes
                );

            var filtered = ApplyTab(rangeQuery, tab);
            filtered = ApplyWorkflowStatusFilter(filtered, workflowStatuses);
            filtered = ApplySearch(filtered, query.Q);
            filtered = ApplySentimentFilter(filtered, sentiments);
            filtered = ApplyDetectedTagFilter(filtered, tagKeys);
            filtered = ApplyContactFilter(filtered, contact);
            filtered = await ApplyQrSourceFilterAsync(
                filtered,
                catalogTypes,
                digitalLinkIds,
                cancellationToken
            );

            if (filterFrom.HasValue && filterTo.HasValue)
            {
                var from = filterFrom.Value;
                var to = filterTo.Value;
                filtered = filtered.Where(f =>
                    f.CreatedAt >= from && f.CreatedAt < to
                );
            }

            return filtered;
        }

        private static string NormalizeExportScope(string? scope)
        {
            var key = (scope ?? "current").Trim().ToLowerInvariant();
            if (key is not ("current" or "all-in-period"))
            {
                throw new ArgumentException(
                    "scope must be current or all-in-period."
                );
            }

            return key;
        }

        private static string NormalizeExportFormat(string? format)
        {
            var key = (format ?? "xlsx").Trim().ToLowerInvariant();
            if (key is not ("xlsx" or "csv"))
            {
                throw new ArgumentException("format must be xlsx or csv.");
            }

            return key;
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (pageSize != DefaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {DefaultPageSize}."
                );
            }
        }

        private static string NormalizeTab(string? tab)
        {
            var key = (tab ?? "all").Trim().ToLowerInvariant();
            if (!AllowedTabs.Contains(key))
            {
                throw new ArgumentException(
                    "tab must be all, needs-attention, new, in-progress, or resolved."
                );
            }

            return key;
        }

        private static string NormalizeSort(string? sort)
        {
            var key = (sort ?? "newest-submitted").Trim().ToLowerInvariant();
            if (!AllowedSorts.Contains(key))
            {
                throw new ArgumentException("Invalid sort value.");
            }

            return key;
        }

        private static List<FeedbackSentiment> NormalizeSentiments(
            string[]? sentiment
        )
        {
            if (sentiment is not { Length: > 0 })
            {
                return [];
            }

            GuestsFilterOptions.Validate(
                marketing: [],
                contact: [],
                sentiment: sentiment
            );

            return GuestsFilterOptions
                .Normalize(sentiment, GuestsFilterOptions.Sentiment)
                .Select(wire =>
                {
                    if (!FeedbackClassificationMapping.TryParseWireSentiment(
                            wire,
                            out var value
                        ))
                    {
                        throw new ArgumentException("Invalid sentiment value.");
                    }

                    return value;
                })
                .Distinct()
                .ToList();
        }

        private static List<FeedbackWorkflowStatus> NormalizeWorkflowStatuses(
            string[]? workflowStatus
        )
        {
            if (workflowStatus is not { Length: > 0 })
            {
                return [];
            }

            var statuses = new List<FeedbackWorkflowStatus>();
            foreach (var raw in workflowStatus)
            {
                if (!FeedbackWorkflowStatusMapping.TryParseWire(raw, out var status))
                {
                    throw new ArgumentException(
                        "workflowStatus must be new, in_progress, or resolved."
                    );
                }

                if (!statuses.Contains(status))
                {
                    statuses.Add(status);
                }
            }

            return statuses;
        }

        private static HashSet<string> NormalizeDetectedTags(
            string[]? detectedTags
        )
        {
            if (detectedTags is not { Length: > 0 })
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var allowed = Enum.GetNames<DetectedTag>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var raw in detectedTags)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (!allowed.Contains(raw))
                {
                    throw new ArgumentException(
                        $"Invalid detectedTags value '{raw}'."
                    );
                }

                var canonical = Enum.GetNames<DetectedTag>()
                    .Single(name =>
                        name.Equals(raw, StringComparison.OrdinalIgnoreCase)
                    );
                result.Add(canonical);
            }

            return result;
        }

        private static HashSet<string> NormalizeContact(string[]? contact)
        {
            if (contact is not { Length: > 0 })
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            GuestsFilterOptions.Validate(
                marketing: [],
                contact: contact,
                sentiment: []
            );

            return GuestsFilterOptions
                .Normalize(contact, GuestsFilterOptions.Contact)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static (
            HashSet<QrType> CatalogTypes,
            HashSet<int> DigitalLinkIds
        ) NormalizeQrSources(string[]? qrSource)
        {
            var catalog = new HashSet<QrType>();
            var digitalIds = new HashSet<int>();

            if (qrSource is not { Length: > 0 })
            {
                return (catalog, digitalIds);
            }

            foreach (var raw in qrSource)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var value = raw.Trim();
                if (value.StartsWith("dgl:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(value[4..], out var id) || id <= 0)
                    {
                        throw new ArgumentException(
                            $"Invalid qrSource value '{raw}'."
                        );
                    }

                    digitalIds.Add(id);
                    continue;
                }

                if (!CatalogQrSources.TryGetValue(value, out var qrType))
                {
                    throw new ArgumentException(
                        $"Invalid qrSource value '{raw}'."
                    );
                }

                catalog.Add(qrType);
            }

            return (catalog, digitalIds);
        }

        private static async Task<FeedbackInboxTabCountsDto> ComputeTabCountsAsync(
            IQueryable<Feedback> rangeQuery,
            CancellationToken cancellationToken
        )
        {
            var rows = await rangeQuery
                .Select(f => new
                {
                    f.ClassificationStatus,
                    f.Sentiment,
                    f.WorkflowStatus,
                })
                .ToListAsync(cancellationToken);

            return new FeedbackInboxTabCountsDto
            {
                All = rows.Count,
                NeedsAttention = rows.Count(r =>
                    r.ClassificationStatus == ClassificationStatus.Succeeded
                    && r.Sentiment == FeedbackSentiment.Negative
                    && r.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                ),
                New = rows.Count(r =>
                    r.WorkflowStatus == FeedbackWorkflowStatus.New
                ),
                InProgress = rows.Count(r =>
                    r.WorkflowStatus == FeedbackWorkflowStatus.InProgress
                ),
                Resolved = rows.Count(r =>
                    r.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                ),
            };
        }

        private static IQueryable<Feedback> ApplyTab(
            IQueryable<Feedback> query,
            string tab
        ) =>
            tab switch
            {
                "needs-attention" => query.Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment == FeedbackSentiment.Negative
                    && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                ),
                "new" => query.Where(f =>
                    f.WorkflowStatus == FeedbackWorkflowStatus.New
                ),
                "in-progress" => query.Where(f =>
                    f.WorkflowStatus == FeedbackWorkflowStatus.InProgress
                ),
                "resolved" => query.Where(f =>
                    f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                ),
                _ => query,
            };

        private static IQueryable<Feedback> ApplyWorkflowStatusFilter(
            IQueryable<Feedback> query,
            IReadOnlyList<FeedbackWorkflowStatus> statuses
        )
        {
            if (statuses.Count == 0)
            {
                return query;
            }

            return query.Where(f => statuses.Contains(f.WorkflowStatus));
        }

        private static IQueryable<Feedback> ApplySearch(
            IQueryable<Feedback> query,
            string? q
        )
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return query;
            }

            var needle = q.Trim();
            var matchingTagKeys = DetectedTagLabels
                .Where(pair =>
                    pair.Label.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Contains(needle, StringComparison.OrdinalIgnoreCase)
                )
                .Select(pair => pair.Key)
                .ToList();

            return query.Where(f =>
                f.Comment.Contains(needle)
                || f.GuestName.Contains(needle)
                || (
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.DetectedTagsJson != null
                    && (
                        matchingTagKeys.Contains(nameof(DetectedTag.FoodQuality))
                            && f.DetectedTagsJson.Contains("\"FoodQuality\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Service))
                            && f.DetectedTagsJson.Contains("\"Service\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.WaitTime))
                            && f.DetectedTagsJson.Contains("\"WaitTime\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.Cleanliness)
                            )
                            && f.DetectedTagsJson.Contains("\"Cleanliness\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Value))
                            && f.DetectedTagsJson.Contains("\"Value\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.Atmosphere)
                            )
                            && f.DetectedTagsJson.Contains("\"Atmosphere\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Billing))
                            && f.DetectedTagsJson.Contains("\"Billing\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.AllergiesDietary)
                            )
                            && f.DetectedTagsJson.Contains("\"AllergiesDietary\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.BookingSeating)
                            )
                            && f.DetectedTagsJson.Contains("\"BookingSeating\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Other))
                            && f.DetectedTagsJson.Contains("\"Other\"")
                    )
                )
            );
        }

        private static IQueryable<Feedback> ApplySentimentFilter(
            IQueryable<Feedback> query,
            IReadOnlyList<FeedbackSentiment> sentiments
        )
        {
            if (sentiments.Count == 0)
            {
                return query;
            }

            return query.Where(f =>
                f.ClassificationStatus == ClassificationStatus.Succeeded
                && f.Sentiment != null
                && sentiments.Contains(f.Sentiment.Value)
            );
        }

        private static IQueryable<Feedback> ApplyDetectedTagFilter(
            IQueryable<Feedback> query,
            HashSet<string> tagKeys
        )
        {
            if (tagKeys.Count == 0)
            {
                return query;
            }

            var includeFoodQuality = tagKeys.Contains(nameof(DetectedTag.FoodQuality));
            var includeService = tagKeys.Contains(nameof(DetectedTag.Service));
            var includeWaitTime = tagKeys.Contains(nameof(DetectedTag.WaitTime));
            var includeCleanliness = tagKeys.Contains(
                nameof(DetectedTag.Cleanliness)
            );
            var includeValue = tagKeys.Contains(nameof(DetectedTag.Value));
            var includeAtmosphere = tagKeys.Contains(
                nameof(DetectedTag.Atmosphere)
            );
            var includeBilling = tagKeys.Contains(nameof(DetectedTag.Billing));
            var includeAllergiesDietary = tagKeys.Contains(
                nameof(DetectedTag.AllergiesDietary)
            );
            var includeBookingSeating = tagKeys.Contains(
                nameof(DetectedTag.BookingSeating)
            );
            var includeOther = tagKeys.Contains(nameof(DetectedTag.Other));

            return query.Where(f =>
                f.ClassificationStatus == ClassificationStatus.Succeeded
                && f.DetectedTagsJson != null
                && (
                    (includeFoodQuality
                        && f.DetectedTagsJson.Contains("\"FoodQuality\""))
                    || (includeService
                        && f.DetectedTagsJson.Contains("\"Service\""))
                    || (includeWaitTime
                        && f.DetectedTagsJson.Contains("\"WaitTime\""))
                    || (includeCleanliness
                        && f.DetectedTagsJson.Contains("\"Cleanliness\""))
                    || (includeValue
                        && f.DetectedTagsJson.Contains("\"Value\""))
                    || (includeAtmosphere
                        && f.DetectedTagsJson.Contains("\"Atmosphere\""))
                    || (includeBilling
                        && f.DetectedTagsJson.Contains("\"Billing\""))
                    || (includeAllergiesDietary
                        && f.DetectedTagsJson.Contains("\"AllergiesDietary\""))
                    || (includeBookingSeating
                        && f.DetectedTagsJson.Contains("\"BookingSeating\""))
                    || (includeOther
                        && f.DetectedTagsJson.Contains("\"Other\""))
                )
            );
        }

        private static IQueryable<Feedback> ApplyContactFilter(
            IQueryable<Feedback> query,
            HashSet<string> contact
        )
        {
            if (contact.Count == 0)
            {
                return query;
            }

            var includeEmail = contact.Contains("email");
            var includeMobile = contact.Contains("mobile");

            if (includeEmail && includeMobile)
            {
                return query.Where(f =>
                    f.ContactType == ContactType.Email
                    || f.ContactType == ContactType.Phone
                );
            }

            if (includeEmail)
            {
                return query.Where(f => f.ContactType == ContactType.Email);
            }

            if (includeMobile)
            {
                return query.Where(f => f.ContactType == ContactType.Phone);
            }

            return query;
        }

        private async Task<IQueryable<Feedback>> ApplyQrSourceFilterAsync(
            IQueryable<Feedback> query,
            HashSet<QrType> catalogTypes,
            HashSet<int> digitalLinkIds,
            CancellationToken cancellationToken
        )
        {
            if (catalogTypes.Count == 0 && digitalLinkIds.Count == 0)
            {
                return query;
            }

            var matchingQrIds = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    (catalogTypes.Count > 0
                        && catalogTypes.Contains(q.QrType)
                        && q.QrType != QrType.DigitalGuestLink)
                    || (digitalLinkIds.Count > 0
                        && digitalLinkIds.Contains(q.Id)
                        && q.QrType == QrType.DigitalGuestLink)
                )
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            return query.Where(f => matchingQrIds.Contains(f.QrCodeId));
        }

        private async Task<List<int>> OrderAndPageAsync(
            IQueryable<Feedback> query,
            string sort,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            IQueryable<int> ordered = sort switch
            {
                "oldest-submitted" => query
                    .OrderBy(f => f.CreatedAt)
                    .ThenBy(f => f.Id)
                    .Select(f => f.Id),
                "needs-attention-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                        && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "oldest-unresolved" => query
                    .OrderBy(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? 1
                            : 0
                    )
                    .ThenBy(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? DateTime.MinValue
                            : f.CreatedAt
                    )
                    .ThenByDescending(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? f.CreatedAt
                            : DateTime.MinValue
                    )
                    .ThenBy(f => f.Id)
                    .Select(f => f.Id),
                "negative-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "positive-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Positive
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "guest-name-az" => query
                    .OrderBy(f =>
                        string.IsNullOrWhiteSpace(f.GuestName) ? 1 : 0
                    )
                    .ThenBy(f => f.GuestName)
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                _ => query
                    .OrderByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
            };

            return await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        private async Task<List<int>> PageRecentlyUpdatedAsync(
            IQueryable<Feedback> query,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            var candidates = await query
                .Select(f => new { f.Id, f.CreatedAt })
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return [];
            }

            var ids = candidates.Select(c => c.Id).ToList();

            var workflowMax = await _context.FeedbackWorkflowStatusChanges
                .AsNoTracking()
                .Where(c => ids.Contains(c.FeedbackId))
                .GroupBy(c => c.FeedbackId)
                .Select(g => new
                {
                    FeedbackId = g.Key,
                    At = g.Max(x => x.CreatedAt),
                })
                .ToDictionaryAsync(
                    x => x.FeedbackId,
                    x => x.At,
                    cancellationToken
                );

            var noteRows = await _context.FeedbackInternalNotes
                .AsNoTracking()
                .Where(n => ids.Contains(n.FeedbackId))
                .Select(n => new
                {
                    n.FeedbackId,
                    n.CreatedAt,
                    n.UpdatedAt,
                    n.DeletedAt,
                })
                .ToListAsync(cancellationToken);

            var noteMax = noteRows
                .GroupBy(n => n.FeedbackId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(x =>
                        x.DeletedAt
                        ?? x.UpdatedAt
                        ?? x.CreatedAt
                    )
                );

            return candidates
                .Select(c =>
                {
                    var updated = c.CreatedAt;
                    if (workflowMax.TryGetValue(c.Id, out var workflowAt)
                        && workflowAt > updated)
                    {
                        updated = workflowAt;
                    }

                    if (noteMax.TryGetValue(c.Id, out var noteAt)
                        && noteAt > updated)
                    {
                        updated = noteAt;
                    }

                    return (c.Id, Updated: updated);
                })
                .OrderByDescending(x => x.Updated)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        private async Task<List<int>> OrderAllAsync(
            IQueryable<Feedback> query,
            string sort,
            CancellationToken cancellationToken
        )
        {
            IQueryable<int> ordered = sort switch
            {
                "oldest-submitted" => query
                    .OrderBy(f => f.CreatedAt)
                    .ThenBy(f => f.Id)
                    .Select(f => f.Id),
                "needs-attention-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                        && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "oldest-unresolved" => query
                    .OrderBy(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? 1
                            : 0
                    )
                    .ThenBy(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? DateTime.MinValue
                            : f.CreatedAt
                    )
                    .ThenByDescending(f =>
                        f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                            ? f.CreatedAt
                            : DateTime.MinValue
                    )
                    .ThenBy(f => f.Id)
                    .Select(f => f.Id),
                "negative-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "positive-first" => query
                    .OrderByDescending(f =>
                        f.ClassificationStatus == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Positive
                    )
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                "guest-name-az" => query
                    .OrderBy(f =>
                        string.IsNullOrWhiteSpace(f.GuestName) ? 1 : 0
                    )
                    .ThenBy(f => f.GuestName)
                    .ThenByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
                _ => query
                    .OrderByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id)
                    .Select(f => f.Id),
            };

            return await ordered.ToListAsync(cancellationToken);
        }

        private async Task<List<int>> OrderRecentlyUpdatedAllAsync(
            IQueryable<Feedback> query,
            CancellationToken cancellationToken
        )
        {
            var candidates = await query
                .Select(f => new { f.Id, f.CreatedAt })
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return [];
            }

            var ids = candidates.Select(c => c.Id).ToList();

            var workflowMax = await _context.FeedbackWorkflowStatusChanges
                .AsNoTracking()
                .Where(c => ids.Contains(c.FeedbackId))
                .GroupBy(c => c.FeedbackId)
                .Select(g => new
                {
                    FeedbackId = g.Key,
                    At = g.Max(x => x.CreatedAt),
                })
                .ToDictionaryAsync(
                    x => x.FeedbackId,
                    x => x.At,
                    cancellationToken
                );

            var noteRows = await _context.FeedbackInternalNotes
                .AsNoTracking()
                .Where(n => ids.Contains(n.FeedbackId))
                .Select(n => new
                {
                    n.FeedbackId,
                    n.CreatedAt,
                    n.UpdatedAt,
                    n.DeletedAt,
                })
                .ToListAsync(cancellationToken);

            var noteMax = noteRows
                .GroupBy(n => n.FeedbackId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(x =>
                        x.DeletedAt
                        ?? x.UpdatedAt
                        ?? x.CreatedAt
                    )
                );

            return candidates
                .Select(c =>
                {
                    var updated = c.CreatedAt;
                    if (workflowMax.TryGetValue(c.Id, out var workflowAt)
                        && workflowAt > updated)
                    {
                        updated = workflowAt;
                    }

                    if (noteMax.TryGetValue(c.Id, out var noteAt)
                        && noteAt > updated)
                    {
                        updated = noteAt;
                    }

                    return (c.Id, Updated: updated);
                })
                .OrderByDescending(x => x.Updated)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .ToList();
        }

        private async Task<List<IReadOnlyList<string>>> MaterializeExportRowsAsync(
            IReadOnlyList<int> orderedIds,
            string locationName,
            bool includeGuestContact,
            CancellationToken cancellationToken
        )
        {
            if (orderedIds.Count == 0)
            {
                return [];
            }

            var rows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => orderedIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            var byId = rows.ToDictionary(f => f.Id);
            var qrIds = rows.Select(f => f.QrCodeId).Distinct().ToList();
            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q => qrIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, cancellationToken);

            return orderedIds
                .Where(id => byId.ContainsKey(id))
                .Select(id =>
                {
                    var feedback = byId[id];
                    var classification =
                        FeedbackClassificationMapping.ToApiFields(feedback);
                    qrCodes.TryGetValue(feedback.QrCodeId, out var qrCode);

                    var issueTags = FormatExportIssueTags(
                        classification.DetectedTags
                    );
                    var guestResponse = FormatExportGuestResponse(
                        classification.Sentiment
                    );
                    var workflowStatus = FormatExportWorkflowStatus(
                        feedback.WorkflowStatus
                    );
                    var needsAttention =
                        FeedbackWorkflowStatusMapping.NeedsAttention(feedback)
                            ? "Yes"
                            : "No";

                    var cells = new List<string>
                    {
                        feedback.Id.ToString(),
                        FormatIsoUtc(feedback.CreatedAt),
                        feedback.Comment ?? string.Empty,
                        guestResponse,
                        classification.ClassificationStatus,
                        issueTags,
                        locationName,
                        FeedbackQrSourceMapping.ToDisplay(qrCode)
                            ?? string.Empty,
                        workflowStatus,
                        needsAttention,
                    };

                    if (includeGuestContact)
                    {
                        cells.Add(feedback.GuestName ?? string.Empty);
                        cells.Add(
                            feedback.ContactType == ContactType.Email
                                ? feedback.GuestContact ?? string.Empty
                                : string.Empty
                        );
                        cells.Add(
                            feedback.ContactType == ContactType.Phone
                                ? feedback.GuestContact ?? string.Empty
                                : string.Empty
                        );
                    }

                    return (IReadOnlyList<string>)cells;
                })
                .ToList();
        }

        private static string FormatExportIssueTags(
            IReadOnlyList<string>? detectedTagKeys
        )
        {
            if (detectedTagKeys is not { Count: > 0 })
            {
                return string.Empty;
            }

            var labels = new List<string>();
            foreach (var key in detectedTagKeys)
            {
                if (TummlyBackend.Helpers.DetectedTagLabels.TryParseKey(
                        key,
                        out var tag
                    ))
                {
                    labels.Add(
                        TummlyBackend.Helpers.DetectedTagLabels.For(tag)
                    );
                }
            }

            return string.Join(";", labels);
        }

        private static string FormatExportGuestResponse(string? wireSentiment)
            => wireSentiment switch
            {
                "positive" => "Positive",
                "neutral" => "Neutral",
                "negative" => "Negative",
                _ => string.Empty,
            };

        private static string FormatExportWorkflowStatus(
            FeedbackWorkflowStatus status
        )
            => status switch
            {
                FeedbackWorkflowStatus.New => "New",
                FeedbackWorkflowStatus.InProgress => "In progress",
                FeedbackWorkflowStatus.Resolved => "Resolved",
                _ => "New",
            };

        private static string FormatIsoUtc(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            return utc.ToString("O");
        }

        private async Task<List<FeedbackInboxListItemDto>> MaterializeItemsAsync(
            IReadOnlyList<int> orderedIds,
            string locationName,
            CancellationToken cancellationToken
        )
        {
            if (orderedIds.Count == 0)
            {
                return [];
            }

            var rows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => orderedIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            var byId = rows.ToDictionary(f => f.Id);
            var qrIds = rows.Select(f => f.QrCodeId).Distinct().ToList();
            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q => qrIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, cancellationToken);

            return orderedIds
                .Where(id => byId.ContainsKey(id))
                .Select(id =>
                {
                    var feedback = byId[id];
                    var classification =
                        FeedbackClassificationMapping.ToApiFields(feedback);
                    qrCodes.TryGetValue(feedback.QrCodeId, out var qrCode);

                    return new FeedbackInboxListItemDto
                    {
                        Id = feedback.Id,
                        CreatedAt = feedback.CreatedAt,
                        Comment = feedback.Comment,
                        GuestName = feedback.GuestName,
                        ContactType = feedback.ContactType.ToString(),
                        LocationName = locationName,
                        QrSource = FeedbackQrSourceMapping.ToDisplay(qrCode),
                        ClassificationStatus =
                            classification.ClassificationStatus,
                        Sentiment = classification.Sentiment,
                        DetectedTags = classification.DetectedTags,
                        WorkflowStatus =
                            FeedbackWorkflowStatusMapping.ToWire(
                                feedback.WorkflowStatus
                            ),
                        NeedsAttention =
                            FeedbackWorkflowStatusMapping.NeedsAttention(
                                feedback
                            ),
                        LocationGuestId = feedback.LocationGuestId,
                    };
                })
                .ToList();
        }
    }
}
