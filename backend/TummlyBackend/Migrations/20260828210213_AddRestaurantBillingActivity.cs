using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantBillingActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantBillingActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Qty = table.Column<int>(type: "int", nullable: true),
                    CampaignName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreditNoteNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Plan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Cadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ScheduledDateLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManualAdjustDirection = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ConsumeSource = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FromPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FromCadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ToPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ToCadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantBillingActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantBillingActivities_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantBillingActivities_RestaurantId_OccurredAtUtc_Id",
                table: "RestaurantBillingActivities",
                columns: new[] { "RestaurantId", "OccurredAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantBillingActivities");
        }
    }
}
