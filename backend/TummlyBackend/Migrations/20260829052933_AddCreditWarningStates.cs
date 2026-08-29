using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditWarningStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditWarningStates",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    HighestBandThisPeriod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditWarningStates", x => new { x.RestaurantId, x.Channel });
                    table.ForeignKey(
                        name: "FK_CreditWarningStates_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditWarningStates");
        }
    }
}
