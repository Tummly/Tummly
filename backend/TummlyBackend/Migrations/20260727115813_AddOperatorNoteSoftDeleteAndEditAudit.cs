using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorNoteSoftDeleteAndEditAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LocationGuestNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByDisplayName",
                table: "LocationGuestNotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "LocationGuestNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEditedByDisplayName",
                table: "LocationGuestNotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastEditedByUserId",
                table: "LocationGuestNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LocationGuestNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FeedbackInternalNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByDisplayName",
                table: "FeedbackInternalNotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "FeedbackInternalNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEditedByDisplayName",
                table: "FeedbackInternalNotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastEditedByUserId",
                table: "FeedbackInternalNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FeedbackInternalNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestNotes_DeletedByUserId",
                table: "LocationGuestNotes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestNotes_LastEditedByUserId",
                table: "LocationGuestNotes",
                column: "LastEditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackInternalNotes_DeletedByUserId",
                table: "FeedbackInternalNotes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackInternalNotes_LastEditedByUserId",
                table: "FeedbackInternalNotes",
                column: "LastEditedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackInternalNotes_Users_DeletedByUserId",
                table: "FeedbackInternalNotes",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackInternalNotes_Users_LastEditedByUserId",
                table: "FeedbackInternalNotes",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationGuestNotes_Users_DeletedByUserId",
                table: "LocationGuestNotes",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationGuestNotes_Users_LastEditedByUserId",
                table: "LocationGuestNotes",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackInternalNotes_Users_DeletedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackInternalNotes_Users_LastEditedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationGuestNotes_Users_DeletedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationGuestNotes_Users_LastEditedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropIndex(
                name: "IX_LocationGuestNotes_DeletedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropIndex(
                name: "IX_LocationGuestNotes_LastEditedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackInternalNotes_DeletedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackInternalNotes_LastEditedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByDisplayName",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "LastEditedByDisplayName",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LocationGuestNotes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByDisplayName",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "LastEditedByDisplayName",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "FeedbackInternalNotes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FeedbackInternalNotes");
        }
    }
}
