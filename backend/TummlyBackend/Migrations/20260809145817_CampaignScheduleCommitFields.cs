using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CampaignScheduleCommitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingReservationRef",
                table: "Campaigns",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservedEstimate",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleMode",
                table: "Campaigns",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleTimeZone",
                table: "Campaigns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAtUtc",
                table: "Campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampaignFrozenRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    LocationGuestId = table.Column<int>(type: "int", nullable: false),
                    FrozenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignFrozenRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignFrozenRecipients_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignFrozenRecipients_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignFrozenRecipients_CampaignId_LocationGuestId",
                table: "CampaignFrozenRecipients",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignFrozenRecipients_LocationGuestId",
                table: "CampaignFrozenRecipients",
                column: "LocationGuestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignFrozenRecipients");

            migrationBuilder.DropColumn(
                name: "BillingReservationRef",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ReservedEstimate",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ScheduleMode",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ScheduleTimeZone",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                table: "Campaigns");
        }
    }
}
