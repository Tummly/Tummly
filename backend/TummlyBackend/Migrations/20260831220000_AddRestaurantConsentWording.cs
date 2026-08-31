using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TummlyBackend.Data;

#nullable disable

namespace TummlyBackend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260831220000_AddRestaurantConsentWording")]
    public partial class AddRestaurantConsentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConsentWording",
                table: "Restaurants",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsConsentWording",
                table: "Restaurants",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConsentWording",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "SmsConsentWording",
                table: "Restaurants");
        }
    }
}
