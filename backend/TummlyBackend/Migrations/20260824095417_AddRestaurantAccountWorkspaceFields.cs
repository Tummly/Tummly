using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantAccountWorkspaceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccountWorkspaceLastSavedAt",
                table: "Restaurants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandLogoContentType",
                table: "Restaurants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandLogoObjectKey",
                table: "Restaurants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceStatus",
                table: "Restaurants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountWorkspaceLastSavedAt",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "BrandLogoContentType",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "BrandLogoObjectKey",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WorkspaceStatus",
                table: "Restaurants");
        }
    }
}
