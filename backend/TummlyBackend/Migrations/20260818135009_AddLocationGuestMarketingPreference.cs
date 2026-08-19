using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationGuestMarketingPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketingPreference",
                table: "LocationGuests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "allowed");

            migrationBuilder.Sql(
                """
                UPDATE LocationGuests
                SET MarketingPreference = CASE
                    WHEN OffersOptOut = 1 THEN 'opted_out'
                    ELSE 'allowed'
                END;
                """
            );

            migrationBuilder.DropColumn(
                name: "OffersOptOut",
                table: "LocationGuests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OffersOptOut",
                table: "LocationGuests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE LocationGuests
                SET OffersOptOut = CASE
                    WHEN MarketingPreference = 'opted_out' THEN 1
                    ELSE 0
                END;
                """
            );

            migrationBuilder.DropColumn(
                name: "MarketingPreference",
                table: "LocationGuests");
        }
    }
}
