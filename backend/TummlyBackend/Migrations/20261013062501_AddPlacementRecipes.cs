using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopMaterialSkus_QrType",
                table: "ShopMaterialSkus");

            migrationBuilder.CreateTable(
                name: "PlacementRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PlacementKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrintRoute = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MintOnFulfilmentDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacementRecipes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlacementRecipes_PlacementKey",
                table: "PlacementRecipes",
                column: "PlacementKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlacementRecipes_Slug",
                table: "PlacementRecipes",
                column: "Slug",
                unique: true);

            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            migrationBuilder.Sql(
                $"""
                INSERT INTO [PlacementRecipes] ([Slug], [Label], [PlacementKey], [PrintRoute], [MintOnFulfilmentDefault], [IsActive], [CreatedAtUtc])
                VALUES
                (N'table-tent', N'Table Tent', N'TableTent', N'EmbeddedPdf', CAST(0 AS bit), CAST(1 AS bit), '{now}'),
                (N'window-sticker', N'Window Sticker', N'WindowSticker', N'EmbeddedPdf', CAST(0 AS bit), CAST(1 AS bit), '{now}'),
                (N'offer-card', N'Offer Card', N'OfferCard', N'EmbeddedPdf', CAST(0 AS bit), CAST(1 AS bit), '{now}');
                """
            );

            migrationBuilder.AddColumn<int>(
                name: "PlacementRecipeId",
                table: "ShopMaterialSkus",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE s
                SET s.[PlacementRecipeId] = r.[Id]
                FROM [ShopMaterialSkus] AS s
                INNER JOIN [PlacementRecipes] AS r ON r.[PlacementKey] = s.[QrType];
                """
            );

            migrationBuilder.AlterColumn<int>(
                name: "PlacementRecipeId",
                table: "ShopMaterialSkus",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_PlacementRecipeId",
                table: "ShopMaterialSkus",
                column: "PlacementRecipeId",
                unique: true,
                filter: "[Status] <> N'Deleted'");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopMaterialSkus_PlacementRecipes_PlacementRecipeId",
                table: "ShopMaterialSkus",
                column: "PlacementRecipeId",
                principalTable: "PlacementRecipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopMaterialSkus_PlacementRecipes_PlacementRecipeId",
                table: "ShopMaterialSkus");

            migrationBuilder.DropTable(
                name: "PlacementRecipes");

            migrationBuilder.DropIndex(
                name: "IX_ShopMaterialSkus_PlacementRecipeId",
                table: "ShopMaterialSkus");

            migrationBuilder.DropColumn(
                name: "PlacementRecipeId",
                table: "ShopMaterialSkus");

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_QrType",
                table: "ShopMaterialSkus",
                column: "QrType",
                unique: true,
                filter: "[Status] <> N'Deleted'");
        }
    }
}
