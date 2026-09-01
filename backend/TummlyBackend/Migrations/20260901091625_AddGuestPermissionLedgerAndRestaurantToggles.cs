using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestPermissionLedgerAndRestaurantToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailMarketingPermissionEnabled",
                table: "Restaurants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeedbackFollowUpPermissionEnabled",
                table: "Restaurants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmsMarketingPermissionEnabled",
                table: "Restaurants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "LocationGuestPermissionLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationGuestId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    PermissionKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationGuestPermissionLedgerEntries", x => x.Id);
                    // NoAction (not Cascade): SQL Server error 1785.
                    // Guest delete removes ledger rows in LocationGuestDeleteService.
                    table.ForeignKey(
                        name: "FK_LocationGuestPermissionLedgerEntries_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id");
                    // NoAction (not Cascade): SQL Server error 1785.
                    table.ForeignKey(
                        name: "FK_LocationGuestPermissionLedgerEntries_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                    // Restrict (not SetNull/Cascade): SQL Server error 1785.
                    table.ForeignKey(
                        name: "FK_LocationGuestPermissionLedgerEntries_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestPermissionLedgerEntries_ActorUserId",
                table: "LocationGuestPermissionLedgerEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestPermissionLedgerEntries_LocationGuestId_PermissionKind_OccurredAt",
                table: "LocationGuestPermissionLedgerEntries",
                columns: new[] { "LocationGuestId", "PermissionKind", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestPermissionLedgerEntries_RestaurantLocationId_OccurredAt",
                table: "LocationGuestPermissionLedgerEntries",
                columns: new[] { "RestaurantLocationId", "OccurredAt" });

            migrationBuilder.Sql(
                """
                UPDATE Restaurants
                SET EmailMarketingPermissionEnabled = 1,
                    SmsMarketingPermissionEnabled = 1,
                    FeedbackFollowUpPermissionEnabled = 1;
                """
            );

            // Backfill permission ledger from legacy MarketingPreference:
            // allowed → grant all three; opted_out → withdraw all three;
            // not_recorded → no ledger rows (state stays not recorded).
            migrationBuilder.Sql(
                """
                INSERT INTO LocationGuestPermissionLedgerEntries
                    (LocationGuestId, RestaurantLocationId, PermissionKind, EventKind, Source, ActorUserId, OccurredAt, CreatedAt)
                SELECT
                    lg.Id,
                    lg.RestaurantLocationId,
                    kinds.PermissionKind,
                    CASE
                        WHEN lg.MarketingPreference = 'allowed' THEN 'grant'
                        ELSE 'withdraw'
                    END,
                    'legacy-marketing-preference',
                    NULL,
                    lg.CreatedAt,
                    GETUTCDATE()
                FROM LocationGuests lg
                CROSS JOIN (
                    VALUES ('email-marketing'), ('sms-marketing'), ('feedback-follow-up')
                ) AS kinds(PermissionKind)
                WHERE lg.MarketingPreference IN ('allowed', 'opted_out');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationGuestPermissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "EmailMarketingPermissionEnabled",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "FeedbackFollowUpPermissionEnabled",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "SmsMarketingPermissionEnabled",
                table: "Restaurants");
        }
    }
}
