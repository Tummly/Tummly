using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent + NoAction FK: SQL Server rejects SetNull here because
            // User -> Restaurant -> RestaurantLocation already forms a cascade path.
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'SelectedLocationId'
)
BEGIN
    ALTER TABLE [Users] ADD [SelectedLocationId] int NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Users_SelectedLocationId' AND object_id = OBJECT_ID(N'[Users]')
)
BEGIN
    CREATE INDEX [IX_Users_SelectedLocationId] ON [Users] ([SelectedLocationId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Users_RestaurantLocations_SelectedLocationId'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_RestaurantLocations_SelectedLocationId]
        FOREIGN KEY ([SelectedLocationId]) REFERENCES [RestaurantLocations] ([Id]) ON DELETE NO ACTION;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_RestaurantLocations_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedLocationId",
                table: "Users");
        }
    }
}
