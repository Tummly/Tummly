using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContractedCatalogVersionId",
                table: "BillingAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomPlanConfigJson",
                table: "BillingAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomPlanConfigJson",
                table: "PendingSignups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommercialCatalogVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VatRateBps = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedByAdminUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialCatalogVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommercialCustomUnitRateRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RateKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnitQuantity = table.Column<int>(type: "int", nullable: false),
                    MonthlyNetPence = table.Column<int>(type: "int", nullable: false),
                    AnnualNetPence = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialCustomUnitRateRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialCustomUnitRateRows_CommercialCatalogVersions_CatalogVersionId",
                        column: x => x.CatalogVersionId,
                        principalTable: "CommercialCatalogVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommercialExtraLocationRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyNetPence = table.Column<int>(type: "int", nullable: false),
                    AnnualNetPence = table.Column<int>(type: "int", nullable: false),
                    UsersAdded = table.Column<int>(type: "int", nullable: false),
                    CreditsMonthlyAi = table.Column<int>(type: "int", nullable: false),
                    CreditsMonthlyEmail = table.Column<int>(type: "int", nullable: false),
                    CreditsMonthlySms = table.Column<int>(type: "int", nullable: false),
                    LookupKeyMonthly = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LookupKeyAnnual = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevolutPlanVariationIdMonthly = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevolutPlanVariationIdAnnual = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialExtraLocationRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialExtraLocationRows_CommercialCatalogVersions_CatalogVersionId",
                        column: x => x.CatalogVersionId,
                        principalTable: "CommercialCatalogVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommercialPlanRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MonthlyNetPence = table.Column<int>(type: "int", nullable: false),
                    AnnualNetPence = table.Column<int>(type: "int", nullable: false),
                    IncludedLocations = table.Column<int>(type: "int", nullable: false),
                    IncludedTeamMembers = table.Column<int>(type: "int", nullable: false),
                    ActiveOffersAccount = table.Column<int>(type: "int", nullable: false),
                    ActiveQrPlacementsPerLocation = table.Column<int>(type: "int", nullable: false),
                    PublishedGuestFormsPerLocation = table.Column<int>(type: "int", nullable: false),
                    DraftGuestFormsPerLocation = table.Column<int>(type: "int", nullable: false),
                    CreditsMonthlyAi = table.Column<int>(type: "int", nullable: true),
                    CreditsMonthlyEmail = table.Column<int>(type: "int", nullable: true),
                    CreditsMonthlySms = table.Column<int>(type: "int", nullable: true),
                    CreditsOneTimeAi = table.Column<int>(type: "int", nullable: true),
                    CreditsOneTimeEmail = table.Column<int>(type: "int", nullable: true),
                    CreditsOneTimeSms = table.Column<int>(type: "int", nullable: true),
                    LookupKeyMonthly = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LookupKeyAnnual = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevolutPlanVariationIdMonthly = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevolutPlanVariationIdAnnual = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SelfServeMaxLocations = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialPlanRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialPlanRows_CommercialCatalogVersions_CatalogVersionId",
                        column: x => x.CatalogVersionId,
                        principalTable: "CommercialCatalogVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommercialTopUpPackRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    NetPence = table.Column<int>(type: "int", nullable: false),
                    LookupKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialTopUpPackRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommercialTopUpPackRows_CommercialCatalogVersions_CatalogVersionId",
                        column: x => x.CatalogVersionId,
                        principalTable: "CommercialCatalogVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccounts_ContractedCatalogVersionId",
                table: "BillingAccounts",
                column: "ContractedCatalogVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCatalogVersions_PublicId",
                table: "CommercialCatalogVersions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCatalogVersions_Status",
                table: "CommercialCatalogVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommercialCustomUnitRateRows_CatalogVersionId_RateKey",
                table: "CommercialCustomUnitRateRows",
                columns: new[] { "CatalogVersionId", "RateKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommercialExtraLocationRows_CatalogVersionId",
                table: "CommercialExtraLocationRows",
                column: "CatalogVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommercialPlanRows_CatalogVersionId_PlanKey",
                table: "CommercialPlanRows",
                columns: new[] { "CatalogVersionId", "PlanKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommercialTopUpPackRows_CatalogVersionId_LookupKey",
                table: "CommercialTopUpPackRows",
                columns: new[] { "CatalogVersionId", "LookupKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingAccounts_CommercialCatalogVersions_ContractedCatalogVersionId",
                table: "BillingAccounts",
                column: "ContractedCatalogVersionId",
                principalTable: "CommercialCatalogVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingAccounts_CommercialCatalogVersions_ContractedCatalogVersionId",
                table: "BillingAccounts");

            migrationBuilder.DropTable(
                name: "CommercialCustomUnitRateRows");

            migrationBuilder.DropTable(
                name: "CommercialExtraLocationRows");

            migrationBuilder.DropTable(
                name: "CommercialPlanRows");

            migrationBuilder.DropTable(
                name: "CommercialTopUpPackRows");

            migrationBuilder.DropTable(
                name: "CommercialCatalogVersions");

            migrationBuilder.DropIndex(
                name: "IX_BillingAccounts_ContractedCatalogVersionId",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ContractedCatalogVersionId",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "CustomPlanConfigJson",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "CustomPlanConfigJson",
                table: "PendingSignups");
        }
    }
}
