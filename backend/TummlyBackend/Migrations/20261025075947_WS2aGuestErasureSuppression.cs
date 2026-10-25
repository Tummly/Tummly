using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class WS2aGuestErasureSuppression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_CampaignId_LocationGuestId",
                table: "OfferIssues");

            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues");

            migrationBuilder.DropIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel",
                table: "CampaignRecipientDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_CampaignFrozenRecipients_CampaignId_LocationGuestId",
                table: "CampaignFrozenRecipients");

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "OfferIssues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "CampaignRecipientDeliveries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "CampaignFrozenRecipients",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "GuestContactSuppressions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    PermissionKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestContactSuppressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestContactSuppressions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_CampaignId_LocationGuestId",
                table: "OfferIssues",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true,
                filter: "[CampaignId] IS NOT NULL AND [LocationGuestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues",
                columns: new[] { "LocationGuestId", "CatalogOfferId" },
                unique: true,
                filter: "[Source] = N'guest_form_thank_you' AND [LocationGuestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel",
                table: "CampaignRecipientDeliveries",
                columns: new[] { "CampaignId", "LocationGuestId", "Channel" },
                unique: true,
                filter: "[LocationGuestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignFrozenRecipients_CampaignId_LocationGuestId",
                table: "CampaignFrozenRecipients",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true,
                filter: "[LocationGuestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GuestContactSuppressions_RestaurantId_PermissionKind_ContactHash",
                table: "GuestContactSuppressions",
                columns: new[] { "RestaurantId", "PermissionKind", "ContactHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestContactSuppressions");

            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_CampaignId_LocationGuestId",
                table: "OfferIssues");

            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues");

            migrationBuilder.DropIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel",
                table: "CampaignRecipientDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_CampaignFrozenRecipients_CampaignId_LocationGuestId",
                table: "CampaignFrozenRecipients");

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "OfferIssues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "CampaignRecipientDeliveries",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LocationGuestId",
                table: "CampaignFrozenRecipients",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_CampaignId_LocationGuestId",
                table: "OfferIssues",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true,
                filter: "[CampaignId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues",
                columns: new[] { "LocationGuestId", "CatalogOfferId" },
                unique: true,
                filter: "[Source] = N'guest_form_thank_you'");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel",
                table: "CampaignRecipientDeliveries",
                columns: new[] { "CampaignId", "LocationGuestId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignFrozenRecipients_CampaignId_LocationGuestId",
                table: "CampaignFrozenRecipients",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true);
        }
    }
}
