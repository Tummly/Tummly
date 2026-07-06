using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpCentreQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpCentreQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmitterName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SubmitterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCentreQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpCentreQueries_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HelpCentreQueries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HelpCentreQueryMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    AuthorKind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorStaffId = table.Column<int>(type: "int", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCentreQueryMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpCentreQueryMessages_HelpCentreQueries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "HelpCentreQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueries_RestaurantLocationId",
                table: "HelpCentreQueries",
                column: "RestaurantLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueries_UserId",
                table: "HelpCentreQueries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueryMessages_QueryId",
                table: "HelpCentreQueryMessages",
                column: "QueryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpCentreQueryMessages");

            migrationBuilder.DropTable(
                name: "HelpCentreQueries");
        }
    }
}
