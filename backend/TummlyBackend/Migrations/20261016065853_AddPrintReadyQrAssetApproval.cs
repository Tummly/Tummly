using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintReadyQrAssetApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "PrintReadyQrAssets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAdminUserId",
                table: "PrintReadyQrAssets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "PrintReadyQrAssets");

            migrationBuilder.DropColumn(
                name: "ApprovedByAdminUserId",
                table: "PrintReadyQrAssets");
        }
    }
}
