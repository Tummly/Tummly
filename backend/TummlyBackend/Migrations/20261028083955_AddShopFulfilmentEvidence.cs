using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopFulfilmentEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliverySource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchCarrierName",
                table: "ShopOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchCarrierReference",
                table: "ShopOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchSource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispatchedByAdminUserId",
                table: "ShopOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparationStartedAtUtc",
                table: "ShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreparationStartedByAdminUserId",
                table: "ShopOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionStartSource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliverySource",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchCarrierName",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchCarrierReference",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchSource",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchedByAdminUserId",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "PreparationStartedAtUtc",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "PreparationStartedByAdminUserId",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "ProductionStartSource",
                table: "ShopOrders");
        }
    }
}
