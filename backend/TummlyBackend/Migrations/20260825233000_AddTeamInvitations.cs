using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "RestaurantAccessActivities",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetDisplayName",
                table: "RestaurantAccessActivities",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetEmail",
                table: "RestaurantAccessActivities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PermissionRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LocationScope = table.Column<int>(type: "int", nullable: false),
                    NamedLocationIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InviterUserId = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpaqueReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PendingPasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInvitations_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamInvitations_Users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvitations_OpaqueReference",
                table: "TeamInvitations",
                column: "OpaqueReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvitations_RestaurantId_Email",
                table: "TeamInvitations",
                columns: new[] { "RestaurantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvitations_InviterUserId",
                table: "TeamInvitations",
                column: "InviterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TeamInvitations");

            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                table: "RestaurantAccessActivities");

            migrationBuilder.DropColumn(
                name: "TargetDisplayName",
                table: "RestaurantAccessActivities");

            migrationBuilder.DropColumn(
                name: "TargetEmail",
                table: "RestaurantAccessActivities");
        }
    }
}
