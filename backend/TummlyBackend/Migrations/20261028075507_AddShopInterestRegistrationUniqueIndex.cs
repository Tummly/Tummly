using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopInterestRegistrationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the oldest registration per user, product and Location.
            // Duplicates would block the unique index below.
            migrationBuilder.Sql(
                """
                WITH Ranked AS
                (
                    SELECT [Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY [UserId], [ShopInterestProductId], [LocationId]
                               ORDER BY [CreatedAtUtc], [Id]) AS [RowNumber]
                    FROM [ShopInterestRegistrations]
                )
                DELETE FROM [ShopInterestRegistrations]
                WHERE [Id] IN (SELECT [Id] FROM Ranked WHERE [RowNumber] > 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_User_Product_Location",
                table: "ShopInterestRegistrations",
                columns: new[] { "UserId", "ShopInterestProductId", "LocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopInterestRegistrations_User_Product_Location",
                table: "ShopInterestRegistrations");
        }
    }
}
