using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantWorkspaceStatusAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WorkspaceStatusChangedAt",
                table: "Restaurants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceStatusChangedByUserId",
                table: "Restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_WorkspaceStatusChangedByUserId",
                table: "Restaurants",
                column: "WorkspaceStatusChangedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Users_WorkspaceStatusChangedByUserId",
                table: "Restaurants",
                column: "WorkspaceStatusChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Users_WorkspaceStatusChangedByUserId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_WorkspaceStatusChangedByUserId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WorkspaceStatusChangedAt",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WorkspaceStatusChangedByUserId",
                table: "Restaurants");
        }
    }
}
