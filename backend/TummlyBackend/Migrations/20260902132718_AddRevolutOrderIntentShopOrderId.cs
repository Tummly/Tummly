using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRevolutOrderIntentShopOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShopOrderId",
                table: "RevolutOrderIntents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevolutOrderIntents_ShopOrderId",
                table: "RevolutOrderIntents",
                column: "ShopOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RevolutOrderIntents_ShopOrderId",
                table: "RevolutOrderIntents");

            migrationBuilder.DropColumn(
                name: "ShopOrderId",
                table: "RevolutOrderIntents");
        }
    }
}
