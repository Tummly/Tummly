using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOrderProductionStarted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProductionStartedAtUtc",
                table: "ShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionStartedByAdminUserId",
                table: "ShopOrders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductionStartedAtUtc",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "ProductionStartedByAdminUserId",
                table: "ShopOrders");
        }
    }
}
