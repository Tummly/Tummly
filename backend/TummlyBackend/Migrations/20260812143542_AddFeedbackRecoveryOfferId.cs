using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackRecoveryOfferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecoveryOfferId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_RecoveryOfferId",
                table: "Feedbacks",
                column: "RecoveryOfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_CatalogOffers_RecoveryOfferId",
                table: "Feedbacks",
                column: "RecoveryOfferId",
                principalTable: "CatalogOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_CatalogOffers_RecoveryOfferId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_RecoveryOfferId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "RecoveryOfferId",
                table: "Feedbacks");
        }
    }
}
