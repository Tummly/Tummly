using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class Lane9e_TrustDraftConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "TrustIndicatorDraftStates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Give each existing draft row its own stamp. The column default above is only a placeholder.
            migrationBuilder.Sql(
                "UPDATE [TrustIndicatorDraftStates] SET [ConcurrencyStamp] = NEWID() WHERE [ConcurrencyStamp] = '00000000-0000-0000-0000-000000000000';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "TrustIndicatorDraftStates");
        }
    }
}
