using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Models;

namespace TummlyBackend.Data
{
    public static class HelpCentreDevSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            ILogger logger
        )
        {
            if (await context.HelpCentreQueries.AnyAsync())
            {
                logger.LogInformation(
                    "Help Centre dev seed skipped — queries already exist."
                );
                return;
            }

            var operatorUser = await context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            int? locationId = null;
            string operatorBusinessName = "Demo Kitchen";

            if (operatorUser != null)
            {
                var restaurant = await context.Restaurants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.OwnerUserId == operatorUser.Id);

                if (restaurant != null)
                {
                    operatorBusinessName = restaurant.Name;

                    locationId = await context.RestaurantLocations
                        .AsNoTracking()
                        .Where(l => l.RestaurantId == restaurant.Id)
                        .OrderBy(l => l.CreatedAt)
                        .Select(l => (int?)l.Id)
                        .FirstOrDefaultAsync();
                }
            }

            var now = DateTime.UtcNow;
            var queries = new List<HelpCentreQuery>();

            if (operatorUser != null)
            {
                queries.AddRange(
                    CreateOperatorQueries(
                        operatorUser,
                        operatorBusinessName,
                        locationId,
                        now
                    )
                );
            }
            else
            {
                logger.LogWarning(
                    "No operator user found — seeding guest Help Centre queries only."
                );
            }

            queries.AddRange(CreateGuestQueries(now));

            context.HelpCentreQueries.AddRange(queries);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Seeded {Count} Help Centre queries for development.",
                queries.Count
            );
        }

        private static IEnumerable<HelpCentreQuery> CreateOperatorQueries(
            User operatorUser,
            string businessName,
            int? locationId,
            DateTime now
        )
        {
            return
            [
                BuildQuery(
                    HelpCentreQueryTopic.QrNotWorking,
                    operatorUser.FullName,
                    operatorUser.Email,
                    businessName,
                    operatorUser.Id,
                    locationId,
                    HelpCentreQueryStatus.New,
                    now.AddDays(-1),
                    [
                        "The QR code at our front counter stopped scanning yesterday. Guests see a blank page when they try to open the menu."
                    ]
                ),
                BuildQuery(
                    HelpCentreQueryTopic.Setup,
                    operatorUser.FullName,
                    operatorUser.Email,
                    businessName,
                    operatorUser.Id,
                    locationId,
                    HelpCentreQueryStatus.InProgress,
                    now.AddDays(-3),
                    [
                        "We finished account setup but I'm not sure how to download the table QR codes for printing.",
                        "Thanks for reaching out — I'll walk you through downloading your QR pack from the dashboard. Open Materials in the left nav, then tap Download QR pack. Let me know if any step is unclear.",
                    ],
                    [
                        HelpCentreQueryAuthorKind.Submitter,
                        HelpCentreQueryAuthorKind.Support,
                    ]
                ),
                BuildQuery(
                    HelpCentreQueryTopic.Billing,
                    operatorUser.FullName,
                    operatorUser.Email,
                    businessName,
                    operatorUser.Id,
                    null,
                    HelpCentreQueryStatus.WaitingOnCustomer,
                    now.AddDays(-2),
                    [
                        "I was charged twice for guest feedback credits last month.",
                        "I can see two credit purchases on 12 May. Can you confirm the invoice numbers you were charged for?",
                    ],
                    [
                        HelpCentreQueryAuthorKind.Submitter,
                        HelpCentreQueryAuthorKind.Support,
                    ]
                ),
                BuildQuery(
                    HelpCentreQueryTopic.GuestFeedback,
                    operatorUser.FullName,
                    operatorUser.Email,
                    businessName,
                    operatorUser.Id,
                    locationId,
                    HelpCentreQueryStatus.Resolved,
                    now.AddDays(-7),
                    [
                        "How do I export guest feedback for last week?",
                        "Open Guest feedback → Export, choose Last 7 days, then CSV. That file includes ratings and comments.",
                        "Perfect, that worked. Thank you!",
                    ],
                    [
                        HelpCentreQueryAuthorKind.Submitter,
                        HelpCentreQueryAuthorKind.Support,
                        HelpCentreQueryAuthorKind.Operator,
                    ]
                ),
                BuildQuery(
                    HelpCentreQueryTopic.Campaign,
                    operatorUser.FullName,
                    operatorUser.Email,
                    businessName,
                    operatorUser.Id,
                    locationId,
                    HelpCentreQueryStatus.EscalatedToAdmin,
                    now.AddHours(-6),
                    [
                        "Our redemption campaign is stuck in review for five days and we go live tomorrow.",
                        "This needs Admin to approve the campaign configuration. I've escalated it with the details below.",
                    ],
                    [
                        HelpCentreQueryAuthorKind.Submitter,
                        HelpCentreQueryAuthorKind.Support,
                    ],
                    escalationNote:
                        "Campaign approval blocked — operator needs go-live tomorrow. Please review in Operator details."
                ),
            ];
        }

        private static IEnumerable<HelpCentreQuery> CreateGuestQueries(
            DateTime now
        )
        {
            return
            [
                BuildQuery(
                    HelpCentreQueryTopic.RequestDemo,
                    "Sam Taylor",
                    "sam.taylor@example.com",
                    "Harbour Street Bistro",
                    userId: null,
                    locationId: null,
                    HelpCentreQueryStatus.New,
                    now.AddHours(-4),
                    [
                        "We run three sites in Manchester and would like a demo of Tummly for our ops team."
                    ],
                    phone: "07700 900123"
                ),
                BuildQuery(
                    HelpCentreQueryTopic.SomethingElse,
                    "Jordan Lee",
                    "jordan.lee@example.com",
                    "Northside Café",
                    userId: null,
                    locationId: null,
                    HelpCentreQueryStatus.Closed,
                    now.AddDays(-14),
                    [
                        "Do you integrate with our existing POS for offer redemptions?",
                        "We don't have a live POS integration in v1. Offers are redeemed through Tummly's guest flow and tracked in your dashboard.",
                        "Understood — we'll revisit next quarter.",
                    ],
                    [
                        HelpCentreQueryAuthorKind.Submitter,
                        HelpCentreQueryAuthorKind.Support,
                        HelpCentreQueryAuthorKind.Submitter,
                    ]
                ),
            ];
        }

        private static HelpCentreQuery BuildQuery(
            HelpCentreQueryTopic topic,
            string submitterName,
            string submitterEmail,
            string businessName,
            int? userId,
            int? locationId,
            HelpCentreQueryStatus status,
            DateTime updatedAt,
            IReadOnlyList<string> messageBodies,
            IReadOnlyList<HelpCentreQueryAuthorKind>? authorKinds = null,
            string? phone = null,
            string? escalationNote = null
        )
        {
            authorKinds ??= [HelpCentreQueryAuthorKind.Submitter];
            var createdAt = updatedAt.AddHours(-messageBodies.Count);

            var query = new HelpCentreQuery
            {
                Topic = topic,
                SubmitterName = submitterName,
                SubmitterEmail = submitterEmail,
                Phone = phone,
                BusinessName = businessName,
                UserId = userId,
                RestaurantLocationId = locationId,
                Status = status,
                EscalationNote = escalationNote,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
            };

            for (var i = 0; i < messageBodies.Count; i++)
            {
                var kind = authorKinds.Count > i
                    ? authorKinds[i]
                    : HelpCentreQueryAuthorKind.Submitter;

                query.Messages.Add(
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = kind,
                        AuthorUserId = kind == HelpCentreQueryAuthorKind.Operator
                            ? userId
                            : null,
                        Body = messageBodies[i],
                        CreatedAt = createdAt.AddHours(i),
                    }
                );
            }

            return query;
        }
    }
}
