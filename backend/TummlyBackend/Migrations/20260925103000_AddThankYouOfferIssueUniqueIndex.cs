using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddThankYouOfferIssueUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues",
                columns: new[] { "LocationGuestId", "CatalogOfferId" },
                unique: true,
                filter: "[Source] = N'guest_form_thank_you'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues");
        }
    }
}
