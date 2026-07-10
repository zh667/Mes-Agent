using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLevelBomAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "BomItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ChildBomId",
                table: "BomItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => x.Id);
                    table.UniqueConstraint("AK_InventoryBalances_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Materials_TenantId_MaterialId",
                        columns: x => new { x.TenantId, x.MaterialId },
                        principalTable: "Materials",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_TenantId_ChildBomId",
                table: "BomItems",
                columns: new[] { "TenantId", "ChildBomId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BomItems_MaterialOrChildBom",
                table: "BomItems",
                sql: "(\"MaterialId\" IS NOT NULL AND \"ChildBomId\" IS NULL) OR (\"MaterialId\" IS NULL AND \"ChildBomId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_TenantId_MaterialId",
                table: "InventoryBalances",
                columns: new[] { "TenantId", "MaterialId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Boms_TenantId_ChildBomId",
                table: "BomItems",
                columns: new[] { "TenantId", "ChildBomId" },
                principalTable: "Boms",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems",
                columns: new[] { "TenantId", "MaterialId" },
                principalTable: "Materials",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Boms_TenantId_ChildBomId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems");

            migrationBuilder.DropTable(
                name: "InventoryBalances");

            migrationBuilder.DropIndex(
                name: "IX_BomItems_TenantId_ChildBomId",
                table: "BomItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BomItems_MaterialOrChildBom",
                table: "BomItems");

            migrationBuilder.DropColumn(
                name: "ChildBomId",
                table: "BomItems");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "BomItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems",
                columns: new[] { "TenantId", "MaterialId" },
                principalTable: "Materials",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
