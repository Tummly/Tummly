using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class Lane4_ShopInterestWithdrawal : Migration
    {
        // Data-only: the three concept names that changed in the seed. A row is changed
        // only while it still holds the old name, so an Admin edit is kept.
        public const string CorrectConceptTitlesSql =
            """
            UPDATE [ShopInterestProducts]
            SET [Title] = N'Standard Packaging Label', [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [ProductKey] = N'standard-package-label' AND [Title] = N'Standard Package Label';

            UPDATE [ShopInterestProducts]
            SET [Title] = N'Drink Cup Seal', [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [ProductKey] = N'drink-cup-seal' AND [Title] = N'Drink / Cup Seal';

            UPDATE [ShopInterestProducts]
            SET [Title] = N'Customer Club Poster', [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [ProductKey] = N'poster-customer-club' AND [Title] = N'Poster / Customer Club';

            UPDATE [ShopInterestRegistrations]
            SET [ProductTitleSnapshot] = N'Standard Packaging Label'
            WHERE [ProductKey] = N'standard-package-label' AND [ProductTitleSnapshot] = N'Standard Package Label';

            UPDATE [ShopInterestRegistrations]
            SET [ProductTitleSnapshot] = N'Drink Cup Seal'
            WHERE [ProductKey] = N'drink-cup-seal' AND [ProductTitleSnapshot] = N'Drink / Cup Seal';

            UPDATE [ShopInterestRegistrations]
            SET [ProductTitleSnapshot] = N'Customer Club Poster'
            WHERE [ProductKey] = N'poster-customer-club' AND [ProductTitleSnapshot] = N'Poster / Customer Club';
            """;

        // Before the unique index: withdraw every active duplicate except the oldest per key.
        // PARTITION BY treats NULL LocationId as one group, the same as the SQL Server index.
        public const string DedupeActiveRegistrationsSql =
            """
            WITH [Ranked] AS (
                SELECT [Id],
                       ROW_NUMBER() OVER (
                           PARTITION BY [ShopInterestProductId], [RestaurantId], [LocationId], [UserId]
                           ORDER BY [CreatedAtUtc], [Id]
                       ) AS [KeyRank]
                FROM [ShopInterestRegistrations]
                WHERE [WithdrawnAtUtc] IS NULL
            )
            UPDATE r
            SET r.[WithdrawnAtUtc] = SYSUTCDATETIME()
            FROM [ShopInterestRegistrations] r
            INNER JOIN [Ranked] k ON k.[Id] = r.[Id]
            WHERE k.[KeyRank] > 1;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopInterestRegistrations_ShopInterestProductId",
                table: "ShopInterestRegistrations");

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAtUtc",
                table: "ShopInterestRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WithdrawnByUserId",
                table: "ShopInterestRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(CorrectConceptTitlesSql);

            migrationBuilder.Sql(DedupeActiveRegistrationsSql);

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_ShopInterestProductId_RestaurantId_LocationId_UserId",
                table: "ShopInterestRegistrations",
                columns: new[] { "ShopInterestProductId", "RestaurantId", "LocationId", "UserId" },
                unique: true,
                filter: "[WithdrawnAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopInterestRegistrations_ShopInterestProductId_RestaurantId_LocationId_UserId",
                table: "ShopInterestRegistrations");

            migrationBuilder.DropColumn(
                name: "WithdrawnAtUtc",
                table: "ShopInterestRegistrations");

            migrationBuilder.DropColumn(
                name: "WithdrawnByUserId",
                table: "ShopInterestRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_ShopInterestProductId",
                table: "ShopInterestRegistrations",
                column: "ShopInterestProductId");
        }
    }
}
