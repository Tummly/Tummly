using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeArchiveAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "QrCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByDisplayName",
                table: "QrCodes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedByUserId",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "QrCodes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "QrCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByDisplayName",
                table: "QrCodes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "QrCodes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ArchivedByDisplayName",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "UpdatedByDisplayName",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "QrCodes");
        }
    }
}
