using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CampaignRecipientDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignRecipientDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    LocationGuestId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignRecipientDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignRecipientDeliveries_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignRecipientDeliveries_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel",
                table: "CampaignRecipientDeliveries",
                columns: new[] { "CampaignId", "LocationGuestId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_LocationGuestId",
                table: "CampaignRecipientDeliveries",
                column: "LocationGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_Outcome_AcceptedAtUtc",
                table: "CampaignRecipientDeliveries",
                columns: new[] { "Outcome", "AcceptedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignRecipientDeliveries");
        }
    }
}
