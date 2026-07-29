using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodesRetireLinkToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             =========================================
             1. CREATE QrCodes TABLE + INDEXES
             =========================================
            */

            migrationBuilder.CreateTable(
                name: "QrCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    QrType = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrCodes_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_Token",
                table: "QrCodes",
                column: "Token",
                unique: true);

            // Filtered unique: at most one Active/Paused QR code per
            // (location, type). Status ints: Active = 0, Paused = 1,
            // Archived = 2 — Archived is excluded so a type can be re-minted.
            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_RestaurantLocationId_QrType",
                table: "QrCodes",
                columns: new[] { "RestaurantLocationId", "QrType" },
                unique: true,
                filter: "[Status] IN (0, 1)");

            /*
             =========================================
             2. BACKFILL: LinkToken -> Smart Guest QrCode
             QrType ints: CounterCard = 0, PackagingSticker = 1,
             DeliveryInsert = 2, WindowSticker = 3, SmartGuest = 4.
             =========================================
            */

            migrationBuilder.Sql(@"
INSERT INTO [QrCodes] ([RestaurantLocationId], [QrType], [Token], [Status], [CreatedAt])
SELECT [Id], 4, [LinkToken], 0, GETUTCDATE()
FROM [RestaurantLocations];");

            /*
             =========================================
             3. MINT FOUR PLACEMENT QR CODES PER LOCATION
             32-char hex tokens (Guid "N" shape). Prefer REPLACE of the
             36-char default uniqueidentifier string over
             CONVERT(..., NEWID(), 2) into nvarchar(32) — Azure SQL can
             still emit the hyphenated form and overflow (error 8115),
             which blocked QA readiness for this migration.
             CROSS APPLY so NEWID() is evaluated once per output row.
             The unique index above is the real uniqueness guarantee.
             =========================================
            */

            migrationBuilder.Sql(@"
INSERT INTO [QrCodes] ([RestaurantLocationId], [QrType], [Token], [Status], [CreatedAt])
SELECT loc.[Id], placementType.[Value], tok.[Token], 0, GETUTCDATE()
FROM [RestaurantLocations] AS loc
CROSS JOIN (VALUES (0), (1), (2), (3)) AS placementType([Value])
CROSS APPLY (
    SELECT REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'') AS [Token]
) AS tok;");

            /*
             =========================================
             4. Feedback.QrCodeId — backfill onto Smart Guest, then require
             =========================================
            */

            migrationBuilder.AddColumn<int>(
                name: "QrCodeId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE f
SET f.[QrCodeId] = q.[Id]
FROM [Feedbacks] f
INNER JOIN [QrCodes] q
    ON q.[RestaurantLocationId] = f.[RestaurantLocationId]
    AND q.[QrType] = 4;");

            migrationBuilder.AlterColumn<int>(
                name: "QrCodeId",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_QrCodeId",
                table: "Feedbacks",
                column: "QrCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_QrCodes_QrCodeId",
                table: "Feedbacks",
                column: "QrCodeId",
                principalTable: "QrCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            /*
             =========================================
             5. QrScanEvent.QrCodeId — backfill onto Smart Guest (stays
             nullable: future scans may predate a resolvable QR code, though
             in practice production rows will always land here).
             =========================================
            */

            migrationBuilder.AddColumn<int>(
                name: "QrCodeId",
                table: "QrScanEvents",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE e
SET e.[QrCodeId] = q.[Id]
FROM [QrScanEvents] e
INNER JOIN [QrCodes] q
    ON q.[RestaurantLocationId] = e.[RestaurantLocationId]
    AND q.[QrType] = 4;");

            migrationBuilder.CreateIndex(
                name: "IX_QrScanEvents_QrCodeId",
                table: "QrScanEvents",
                column: "QrCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_QrScanEvents_QrCodes_QrCodeId",
                table: "QrScanEvents",
                column: "QrCodeId",
                principalTable: "QrCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            /*
             =========================================
             6. DROP LinkToken — every location's Smart Guest QrCode now
             carries the same token, so resolve/uniqueness has fully moved
             to QrCodes.Token before this point.
             =========================================
            */

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_LinkToken",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "LinkToken",
                table: "RestaurantLocations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkToken",
                table: "RestaurantLocations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE l
SET l.[LinkToken] = q.[Token]
FROM [RestaurantLocations] l
INNER JOIN [QrCodes] q
    ON q.[RestaurantLocationId] = l.[Id]
    AND q.[QrType] = 4;");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_LinkToken",
                table: "RestaurantLocations",
                column: "LinkToken",
                unique: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_QrCodes_QrCodeId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_QrScanEvents_QrCodes_QrCodeId",
                table: "QrScanEvents");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_QrCodeId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_QrScanEvents_QrCodeId",
                table: "QrScanEvents");

            migrationBuilder.DropColumn(
                name: "QrCodeId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "QrCodeId",
                table: "QrScanEvents");

            migrationBuilder.DropTable(
                name: "QrCodes");
        }
    }
}
