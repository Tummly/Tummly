using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditLedgerEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservationRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    PricebookVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLedgerEntries", x => x.Id);
                    table.CheckConstraint("CK_CreditLedgerEntries_QuantityPositive", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_CreditLedgerEntries_BillingAccounts_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "BillingAccounts",
                        principalColumn: "RestaurantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditLedgerEntries_CreditLedgerEntries_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "CreditLedgerEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditLedgerEntries_CreditLedgerEntries_ReversedEntryId",
                        column: x => x.ReversedEntryId,
                        principalTable: "CreditLedgerEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditLedgerEntries_RestaurantLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_AllocationId",
                table: "CreditLedgerEntries",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_LocationId",
                table: "CreditLedgerEntries",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_ReservationRef_AllocationId",
                table: "CreditLedgerEntries",
                columns: new[] { "ReservationRef", "AllocationId" },
                unique: true,
                filter: "[EntryType] = N'reservation' AND [ReservationRef] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_RestaurantId_Channel",
                table: "CreditLedgerEntries",
                columns: new[] { "RestaurantId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_ReversedEntryId",
                table: "CreditLedgerEntries",
                column: "ReversedEntryId",
                unique: true,
                filter: "[ReversedEntryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditLedgerEntries");
        }
    }
}
