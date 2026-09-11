using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintReadyQrAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintReadyQrAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    QrType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    QrTokenFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TemplatePackVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OfferCopyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ShopOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintReadyQrAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintReadyQrAssets_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintReadyQrAssets_RestaurantLocationId_QrType",
                table: "PrintReadyQrAssets",
                columns: new[] { "RestaurantLocationId", "QrType" },
                unique: true,
                filter: "[ShopOrderId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintReadyQrAssets");
        }
    }
}
