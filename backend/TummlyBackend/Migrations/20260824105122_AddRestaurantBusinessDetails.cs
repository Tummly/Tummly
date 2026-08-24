using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantBusinessDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantBusinessDetails",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    LegalStructure = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LegalBusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CountryOfRegistration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TownCity = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    County = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantBusinessDetails", x => x.RestaurantId);
                    table.ForeignKey(
                        name: "FK_RestaurantBusinessDetails_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantBusinessDetails");
        }
    }
}
