using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopInterestCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopInterestProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopInterestProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopInterestRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopInterestProductId = table.Column<int>(type: "int", nullable: false),
                    ProductKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProductTitleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    RestaurantNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LocationNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserFullNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopInterestRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopInterestRegistrations_ShopInterestProducts_ShopInterestProductId",
                        column: x => x.ShopInterestProductId,
                        principalTable: "ShopInterestProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestProducts_ProductKey",
                table: "ShopInterestProducts",
                column: "ProductKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_CreatedAtUtc",
                table: "ShopInterestRegistrations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_ProductKey",
                table: "ShopInterestRegistrations",
                column: "ProductKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShopInterestRegistrations_ShopInterestProductId",
                table: "ShopInterestRegistrations",
                column: "ShopInterestProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopInterestRegistrations");

            migrationBuilder.DropTable(
                name: "ShopInterestProducts");
        }
    }
}
