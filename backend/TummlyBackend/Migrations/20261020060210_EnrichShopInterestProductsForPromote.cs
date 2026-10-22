using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class EnrichShopInterestProductsForPromote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "ShopInterestProducts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkedSkuId",
                table: "ShopInterestProducts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Material",
                table: "ShopInterestProducts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ShopInterestProducts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ShopInterestProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopInterestProductId = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopInterestProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopInterestProductImages_ShopInterestProducts_ShopInterestProductId",
                        column: x => x.ShopInterestProductId,
                        principalTable: "ShopInterestProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Carry any legacy ImageUrl into the first image row before drop.
            migrationBuilder.Sql(
                """
                INSERT INTO ShopInterestProductImages
                    (ShopInterestProductId, StorageKey, ContentType, SortOrder, CreatedAtUtc)
                SELECT
                    Id,
                    CASE
                        WHEN ImageUrl LIKE '/api/public/shop-catalog-images/%'
                            THEN 'shop-materials/' + SUBSTRING(ImageUrl, LEN('/api/public/shop-catalog-images/') + 1, 512)
                        ELSE 'legacy:' + ImageUrl
                    END,
                    'image/png',
                    0,
                    SYSUTCDATETIME()
                FROM ShopInterestProducts
                WHERE ImageUrl IS NOT NULL AND LTRIM(RTRIM(ImageUrl)) <> '';
                """
            );

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ShopInterestProducts");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestProducts_LinkedSkuId",
                table: "ShopInterestProducts",
                column: "LinkedSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestProductImages_ShopInterestProductId_SortOrder",
                table: "ShopInterestProductImages",
                columns: new[] { "ShopInterestProductId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopInterestProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ShopInterestProducts_LinkedSkuId",
                table: "ShopInterestProducts");

            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "ShopInterestProducts");

            migrationBuilder.DropColumn(
                name: "LinkedSkuId",
                table: "ShopInterestProducts");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "ShopInterestProducts");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ShopInterestProducts");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ShopInterestProducts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }
    }
}
