using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignRecipientDeliveryOpenTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAtUtc",
                table: "CampaignRecipientDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "CampaignRecipientDeliveries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_OpenedAtUtc",
                table: "CampaignRecipientDeliveries",
                columns: new[] { "CampaignId", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipientDeliveries_ProviderMessageId",
                table: "CampaignRecipientDeliveries",
                column: "ProviderMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampaignRecipientDeliveries_CampaignId_OpenedAtUtc",
                table: "CampaignRecipientDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_CampaignRecipientDeliveries_ProviderMessageId",
                table: "CampaignRecipientDeliveries");

            migrationBuilder.DropColumn(
                name: "OpenedAtUtc",
                table: "CampaignRecipientDeliveries");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "CampaignRecipientDeliveries");
        }
    }
}
