using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedRestaurantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RestaurantMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    PermissionRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LocationScope = table.Column<int>(type: "int", nullable: false),
                    NamedLocationIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantMemberships_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMemberships_RestaurantId",
                table: "RestaurantMemberships",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMemberships_UserId_RestaurantId",
                table: "RestaurantMemberships",
                columns: new[] { "UserId", "RestaurantId" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO RestaurantMemberships
                    (UserId, RestaurantId, PermissionRole, LocationScope, NamedLocationIdsJson, Status)
                SELECT
                    OwnerUserId,
                    Id,
                    N'Owner',
                    0,
                    N'[]',
                    0
                FROM Restaurants
                WHERE OwnerUserId IS NOT NULL
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantMemberships");

            migrationBuilder.DropColumn(
                name: "SelectedRestaurantId",
                table: "Users");
        }
    }
}
