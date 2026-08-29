using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TummlyBackend.Data;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260829120000_AddBillingAccountDunningOutstandingOrderId")]
    public partial class AddBillingAccountDunningOutstandingOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DunningOutstandingOrderId",
                table: "BillingAccounts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DunningOutstandingOrderId",
                table: "BillingAccounts");
        }
    }
}
