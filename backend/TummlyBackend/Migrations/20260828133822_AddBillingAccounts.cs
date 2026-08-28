using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingAccounts",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    BillingEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    LowCreditAlertOwner = table.Column<bool>(type: "bit", nullable: false),
                    LowCreditAlertAdmin = table.Column<bool>(type: "bit", nullable: false),
                    LowCreditAlertBillingContact = table.Column<bool>(type: "bit", nullable: false),
                    PaymentFailureAlertOwner = table.Column<bool>(type: "bit", nullable: false),
                    PaymentFailureAlertBillingContact = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAccounts", x => x.RestaurantId);
                    table.ForeignKey(
                        name: "FK_BillingAccounts_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO BillingAccounts (
                    RestaurantId,
                    LowCreditAlertOwner,
                    LowCreditAlertAdmin,
                    LowCreditAlertBillingContact,
                    PaymentFailureAlertOwner,
                    PaymentFailureAlertBillingContact
                )
                SELECT
                    Id,
                    1,
                    0,
                    1,
                    1,
                    1
                FROM Restaurants
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingAccounts");
        }
    }
}
