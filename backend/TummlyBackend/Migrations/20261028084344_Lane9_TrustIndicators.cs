using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class Lane9_TrustIndicators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustIndicatorDraftStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LastEditedByAdminUserId = table.Column<int>(type: "int", nullable: true),
                    LastEditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByAdminUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustIndicatorDraftStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrustIndicatorPublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedByAdminUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByAdminUserId = table.Column<int>(type: "int", nullable: false),
                    SubmittedByAdminUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustIndicatorPublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrustIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvidenceClass = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Tooltip = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CtaType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CtaTarget = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReviewOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LastEditedByAdminUserId = table.Column<int>(type: "int", nullable: true),
                    LastEditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustIndicators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustIndicatorPublications_IdempotencyKey",
                table: "TrustIndicatorPublications",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustIndicatorPublications_Version",
                table: "TrustIndicatorPublications",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustIndicators_ItemNumber",
                table: "TrustIndicators",
                column: "ItemNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustIndicatorDraftStates");

            migrationBuilder.DropTable(
                name: "TrustIndicatorPublications");

            migrationBuilder.DropTable(
                name: "TrustIndicators");
        }
    }
}
