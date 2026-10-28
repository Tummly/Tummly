using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class Lane5_PilotOriginalEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PilotOriginalEndUtc",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            // Existing Pilot rows: the original end is the effective end less the extension days.
            migrationBuilder.Sql("UPDATE BillingAccounts SET PilotOriginalEndUtc = DATEADD(day, -PilotExtensionDaysTotal, PilotPeriodEnd) WHERE PilotPeriodEnd IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PilotOriginalEndUtc",
                table: "BillingAccounts");
        }
    }
}
