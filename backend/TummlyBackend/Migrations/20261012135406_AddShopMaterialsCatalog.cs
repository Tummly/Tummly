using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopMaterialsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopMaterialSkus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkuId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Material = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    QrType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UnitNetPence = table.Column<int>(type: "int", nullable: false),
                    MinOrderQty = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    MintOnShopFulfilment = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AboutBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecificationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterialSkus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopMaterialTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterialTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopMaterialSkuImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkuId = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterialSkuImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopMaterialSkuImages_ShopMaterialSkus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "ShopMaterialSkus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopMaterialSkuTags",
                columns: table => new
                {
                    SkuId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterialSkuTags", x => new { x.SkuId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ShopMaterialSkuTags_ShopMaterialSkus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "ShopMaterialSkus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopMaterialSkuTags_ShopMaterialTags_TagId",
                        column: x => x.TagId,
                        principalTable: "ShopMaterialTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkuImages_SkuId_SortOrder",
                table: "ShopMaterialSkuImages",
                columns: new[] { "SkuId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_SkuId",
                table: "ShopMaterialSkus",
                column: "SkuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_Status",
                table: "ShopMaterialSkus",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkuTags_TagId",
                table: "ShopMaterialSkuTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialTags_Slug",
                table: "ShopMaterialTags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopMaterialSkuImages");

            migrationBuilder.DropTable(
                name: "ShopMaterialSkuTags");

            migrationBuilder.DropTable(
                name: "ShopMaterialSkus");

            migrationBuilder.DropTable(
                name: "ShopMaterialTags");
        }
    }
}
