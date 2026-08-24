using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantKeyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrivacyContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupportContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Restaurants
                SET BillingContactUserId = OwnerUserId,
                    PrivacyContactUserId = OwnerUserId,
                    SupportContactUserId = OwnerUserId
                WHERE BillingContactUserId IS NULL
                   OR PrivacyContactUserId IS NULL
                   OR SupportContactUserId IS NULL
                """
            );

            migrationBuilder.AlterColumn<int>(
                name: "BillingContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PrivacyContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SupportContactUserId",
                table: "Restaurants",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_BillingContactUserId",
                table: "Restaurants",
                column: "BillingContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_PrivacyContactUserId",
                table: "Restaurants",
                column: "PrivacyContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_SupportContactUserId",
                table: "Restaurants",
                column: "SupportContactUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Users_BillingContactUserId",
                table: "Restaurants",
                column: "BillingContactUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Users_PrivacyContactUserId",
                table: "Restaurants",
                column: "PrivacyContactUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Users_SupportContactUserId",
                table: "Restaurants",
                column: "SupportContactUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Users_BillingContactUserId",
                table: "Restaurants");

            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Users_PrivacyContactUserId",
                table: "Restaurants");

            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Users_SupportContactUserId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_BillingContactUserId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_PrivacyContactUserId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_SupportContactUserId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "BillingContactUserId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "PrivacyContactUserId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "SupportContactUserId",
                table: "Restaurants");
        }
    }
}
