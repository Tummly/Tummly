using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationGuestActivityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationGuestActivityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationGuestId = table.Column<int>(type: "int", nullable: true),
                    FeedbackId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationGuestActivityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationGuestActivityEvents_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LocationGuestActivityEvents_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestActivityEvents_FeedbackId_OccurredAt",
                table: "LocationGuestActivityEvents",
                columns: new[] { "FeedbackId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestActivityEvents_Kind",
                table: "LocationGuestActivityEvents",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestActivityEvents_LocationGuestId_OccurredAt",
                table: "LocationGuestActivityEvents",
                columns: new[] { "LocationGuestId", "OccurredAt" });

            // Idempotent historical backfill. Classification OccurredAt ≈
            // ClassificationClaimedAt if set, else Feedback.CreatedAt.
            migrationBuilder.Sql(
                """
                INSERT INTO LocationGuestActivityEvents
                    (LocationGuestId, FeedbackId, Kind, PayloadJson, OccurredAt, CreatedAt)
                SELECT
                    lg.Id,
                    NULL,
                    N'guest-joined',
                    NULL,
                    lg.CreatedAt,
                    SYSUTCDATETIME()
                FROM LocationGuests lg
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM LocationGuestActivityEvents e
                    WHERE e.LocationGuestId = lg.Id
                      AND e.Kind = N'guest-joined'
                );

                INSERT INTO LocationGuestActivityEvents
                    (LocationGuestId, FeedbackId, Kind, PayloadJson, OccurredAt, CreatedAt)
                SELECT
                    f.LocationGuestId,
                    f.Id,
                    N'feedback',
                    NULL,
                    f.CreatedAt,
                    SYSUTCDATETIME()
                FROM Feedbacks f
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM LocationGuestActivityEvents e
                    WHERE e.FeedbackId = f.Id
                      AND e.Kind = N'feedback'
                );

                INSERT INTO LocationGuestActivityEvents
                    (LocationGuestId, FeedbackId, Kind, PayloadJson, OccurredAt, CreatedAt)
                SELECT
                    m.LocationGuestId,
                    NULL,
                    N'tag-applied',
                    (
                        SELECT
                            t.DisplayName AS tagName,
                            t.Id AS guestTagId
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                    ),
                    m.CreatedAt,
                    SYSUTCDATETIME()
                FROM LocationGuestTags m
                INNER JOIN GuestTags t ON t.Id = m.GuestTagId
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM LocationGuestActivityEvents e
                    WHERE e.LocationGuestId = m.LocationGuestId
                      AND e.Kind = N'tag-applied'
                      AND e.PayloadJson LIKE
                          N'%"guestTagId":' + CAST(m.GuestTagId AS nvarchar(20)) + N'%'
                );

                INSERT INTO LocationGuestActivityEvents
                    (LocationGuestId, FeedbackId, Kind, PayloadJson, OccurredAt, CreatedAt)
                SELECT
                    f.LocationGuestId,
                    f.Id,
                    CASE
                        WHEN f.ClassificationStatus = 1
                            THEN N'classification-succeeded'
                        ELSE N'classification-failed'
                    END,
                    CASE
                        WHEN f.ClassificationStatus = 1 AND f.Sentiment = 0
                            THEN N'{"sentiment":"positive"}'
                        WHEN f.ClassificationStatus = 1 AND f.Sentiment = 1
                            THEN N'{"sentiment":"neutral"}'
                        WHEN f.ClassificationStatus = 1 AND f.Sentiment = 2
                            THEN N'{"sentiment":"negative"}'
                        ELSE NULL
                    END,
                    COALESCE(f.ClassificationClaimedAt, f.CreatedAt),
                    SYSUTCDATETIME()
                FROM Feedbacks f
                WHERE f.ClassificationStatus IN (1, 2)
                  AND NOT EXISTS (
                    SELECT 1
                    FROM LocationGuestActivityEvents e
                    WHERE e.FeedbackId = f.Id
                      AND e.Kind IN (
                          N'classification-succeeded',
                          N'classification-failed'
                      )
                  );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationGuestActivityEvents");
        }
    }
}
