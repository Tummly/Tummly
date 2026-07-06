using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTrialRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE TrialRequests
SET Status = UPPER(REPLACE(LTRIM(RTRIM(COALESCE(Status, 'EMAIL_VERIFIED'))), ' ', '_'));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: original mixed-case status strings are overwritten.
        }
    }
}
