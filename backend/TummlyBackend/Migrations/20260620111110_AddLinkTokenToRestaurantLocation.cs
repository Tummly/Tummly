using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkTokenToRestaurantLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Each Sql() call runs as its own batch. SQL Server cannot reference a column
            // added via ALTER TABLE in the same batch (compile-time name resolution).
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[RestaurantLocations]') AND name = 'LinkToken'
)
BEGIN
    ALTER TABLE [RestaurantLocations] ADD [LinkToken] nvarchar(32) NULL;
END");

            migrationBuilder.Sql(@"
UPDATE [RestaurantLocations]
SET [LinkToken] = CONVERT(nvarchar(32), NEWID(), 2)
WHERE [LinkToken] IS NULL;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[RestaurantLocations]') AND name = 'LinkToken'
    AND is_nullable = 1
)
BEGIN
    ALTER TABLE [RestaurantLocations] ALTER COLUMN [LinkToken] nvarchar(32) NOT NULL;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RestaurantLocations_LinkToken' AND object_id = OBJECT_ID(N'[RestaurantLocations]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_RestaurantLocations_LinkToken] ON [RestaurantLocations] ([LinkToken]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_LinkToken",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "LinkToken",
                table: "RestaurantLocations");
        }
    }
}
