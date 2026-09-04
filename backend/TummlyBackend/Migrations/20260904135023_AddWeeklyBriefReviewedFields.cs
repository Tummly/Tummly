using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyBriefReviewedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "WeeklyBriefs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "WeeklyBriefs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyBriefs_ReviewedByUserId",
                table: "WeeklyBriefs",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyBriefs_Users_ReviewedByUserId",
                table: "WeeklyBriefs",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyBriefs_Users_ReviewedByUserId",
                table: "WeeklyBriefs");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyBriefs_ReviewedByUserId",
                table: "WeeklyBriefs");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "WeeklyBriefs");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "WeeklyBriefs");
        }
    }
}
