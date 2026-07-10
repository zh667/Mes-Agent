using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Boms_BomId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Materials_MaterialId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Boms_Products_ProductId",
                table: "Boms");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_Conversations_ConversationId",
                table: "ConversationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectRecords_DefectTypes_DefectTypeId",
                table: "DefectRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectRecords_QualityInspections_QualityInspectionId",
                table: "DefectRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DowntimeRecords_Equipment_EquipmentId",
                table: "DowntimeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_ProductionLines_ProductionLineId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentAlarms_Equipment_EquipmentId",
                table: "EquipmentAlarms");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStatuses_Equipment_EquipmentId",
                table: "EquipmentStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRoutes_Products_ProductId",
                table: "ProcessRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessSteps_ProcessRoutes_ProcessRouteId",
                table: "ProcessSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessSteps_Workstations_WorkstationId",
                table: "ProcessSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_ProcessSteps_ProcessStepId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_WorkOrders_WorkOrderId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_ProcessSteps_ProcessStepId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_WorkOrders_WorkOrderId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_ProcessSteps_ProcessStepId",
                table: "WorkOrderOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_WorkOrders_WorkOrderId",
                table: "WorkOrderOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_ProductionLines_ProductionLineId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Products_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Workstations_ProductionLines_ProductionLineId",
                table: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_Workstations_Code",
                table: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_Workstations_ProductionLineId",
                table: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_Code",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_CreatedAt",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_PlannedEndTime",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_PlannedStartTime",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ProductionLineId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_Status",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_ProcessStepId",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_WorkOrderId",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_BatchNumber",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_Code",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_InspectionTime",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_ProcessStepId",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_WorkOrderId",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_BatchNumber",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_ProcessStepId",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_Timestamp",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_WorkOrderId",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionLines_Code",
                table: "ProductionLines");

            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_ProcessRouteId",
                table: "ProcessSteps");

            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_WorkstationId",
                table: "ProcessSteps");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRoutes_Code",
                table: "ProcessRoutes");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRoutes_ProductId",
                table: "ProcessRoutes");

            migrationBuilder.DropIndex(
                name: "IX_Materials_Code",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentStatuses_EquipmentId_StartTime",
                table: "EquipmentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentAlarms_EquipmentId_OccurredAt",
                table: "EquipmentAlarms");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Code",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_ProductionLineId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_DowntimeRecords_EquipmentId",
                table: "DowntimeRecords");

            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_DocumentId_IsActive",
                table: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_Documents_FileName",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_DocumentId_Sequence",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DefectTypes_Code",
                table: "DefectTypes");

            migrationBuilder.DropIndex(
                name: "IX_DefectRecords_DefectTypeId",
                table: "DefectRecords");

            migrationBuilder.DropIndex(
                name: "IX_DefectRecords_QualityInspectionId",
                table: "DefectRecords");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UpdatedAt",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAt",
                table: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_Boms_Code",
                table: "Boms");

            migrationBuilder.DropIndex(
                name: "IX_Boms_ProductId",
                table: "Boms");

            migrationBuilder.DropIndex(
                name: "IX_BomItems_BomId",
                table: "BomItems");

            migrationBuilder.DropIndex(
                name: "IX_BomItems_MaterialId",
                table: "BomItems");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Workstations",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "WorkOrders",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "WorkOrderOperations",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "QualityInspections",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Products",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ProductionReports",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ProductionLines",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ProcessSteps",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ProcessRoutes",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Materials",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EquipmentStatuses",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "EquipmentAlarms",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Equipment",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DowntimeRecords",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DocumentVersions",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Documents",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DocumentChunks",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DefectTypes",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DefectRecords",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Conversations",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ConversationMessages",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Boms",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "BomItems",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Workstations_TenantId_Id",
                table: "Workstations",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkOrders_TenantId_Id",
                table: "WorkOrders",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkOrderOperations_TenantId_Id",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_QualityInspections_TenantId_Id",
                table: "QualityInspections",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                table: "Products",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProductionReports_TenantId_Id",
                table: "ProductionReports",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProductionLines_TenantId_Id",
                table: "ProductionLines",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProcessSteps_TenantId_Id",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProcessRoutes_TenantId_Id",
                table: "ProcessRoutes",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Materials_TenantId_Id",
                table: "Materials",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_EquipmentStatuses_TenantId_Id",
                table: "EquipmentStatuses",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_EquipmentAlarms_TenantId_Id",
                table: "EquipmentAlarms",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Equipment_TenantId_Id",
                table: "Equipment",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DowntimeRecords_TenantId_Id",
                table: "DowntimeRecords",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DocumentVersions_TenantId_Id",
                table: "DocumentVersions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Documents_TenantId_Id",
                table: "Documents",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DocumentChunks_TenantId_Id",
                table: "DocumentChunks",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DefectTypes_TenantId_Id",
                table: "DefectTypes",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DefectRecords_TenantId_Id",
                table: "DefectRecords",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Conversations_TenantId_Id",
                table: "Conversations",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ConversationMessages_TenantId_Id",
                table: "ConversationMessages",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Boms_TenantId_Id",
                table: "Boms",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_BomItems_TenantId_Id",
                table: "BomItems",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTenantMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTenantMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Tenants" ("Id", "Code", "Name", "IsActive", "CreatedAt")
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    'DEFAULT',
                    'Default Tenant',
                    TRUE,
                    NOW())
                ON CONFLICT ("Id") DO NOTHING;

                DO $$
                DECLARE
                    tenant_table text;
                BEGIN
                    FOREACH tenant_table IN ARRAY ARRAY[
                        'Products', 'Materials', 'Boms', 'BomItems',
                        'ProductionLines', 'ProcessRoutes', 'ProcessSteps', 'Workstations',
                        'WorkOrders', 'WorkOrderOperations', 'ProductionReports',
                        'QualityInspections', 'DefectRecords', 'DefectTypes',
                        'Equipment', 'EquipmentStatuses', 'EquipmentAlarms', 'DowntimeRecords',
                        'Documents', 'DocumentVersions', 'DocumentChunks',
                        'Conversations', 'ConversationMessages'
                    ]
                    LOOP
                        EXECUTE format(
                            'UPDATE %I SET "TenantId" = %L WHERE "TenantId" = %L',
                            tenant_table,
                            '00000000-0000-0000-0000-000000000001',
                            '');
                        EXECUTE format(
                            'ALTER TABLE %I ALTER COLUMN "TenantId" DROP DEFAULT',
                            tenant_table);
                    END LOOP;
                END $$;

                INSERT INTO "UserTenantMemberships"
                    ("UserId", "TenantId", "Role", "IsActive", "CreatedAt")
                SELECT
                    "Id",
                    '00000000-0000-0000-0000-000000000001',
                    "Role",
                    TRUE,
                    NOW()
                FROM "AspNetUsers";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_TenantId_Code",
                table: "Workstations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_TenantId_ProductionLineId",
                table: "Workstations",
                columns: new[] { "TenantId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_Code",
                table: "WorkOrders",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_CreatedAt",
                table: "WorkOrders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_PlannedEndTime",
                table: "WorkOrders",
                columns: new[] { "TenantId", "PlannedEndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_PlannedStartTime",
                table: "WorkOrders",
                columns: new[] { "TenantId", "PlannedStartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_ProductId",
                table: "WorkOrders",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_ProductionLineId",
                table: "WorkOrders",
                columns: new[] { "TenantId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_Status",
                table: "WorkOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_TenantId_ProcessStepId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "ProcessStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_TenantId_BatchNumber",
                table: "QualityInspections",
                columns: new[] { "TenantId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_TenantId_Code",
                table: "QualityInspections",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_TenantId_InspectionTime",
                table: "QualityInspections",
                columns: new[] { "TenantId", "InspectionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_TenantId_ProcessStepId",
                table: "QualityInspections",
                columns: new[] { "TenantId", "ProcessStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_TenantId_WorkOrderId",
                table: "QualityInspections",
                columns: new[] { "TenantId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Code",
                table: "Products",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_TenantId_BatchNumber",
                table: "ProductionReports",
                columns: new[] { "TenantId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_TenantId_EquipmentId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_TenantId_ProcessStepId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "ProcessStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_TenantId_Timestamp",
                table: "ProductionReports",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_TenantId_WorkOrderId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_TenantId_Code",
                table: "ProductionLines",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "ProcessRouteId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_TenantId_WorkstationId",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "WorkstationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRoutes_TenantId_Code",
                table: "ProcessRoutes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRoutes_TenantId_ProductId",
                table: "ProcessRoutes",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_TenantId_Code",
                table: "Materials",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentStatuses_TenantId_EquipmentId_StartTime",
                table: "EquipmentStatuses",
                columns: new[] { "TenantId", "EquipmentId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAlarms_TenantId_EquipmentId_OccurredAt",
                table: "EquipmentAlarms",
                columns: new[] { "TenantId", "EquipmentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_TenantId_Code",
                table: "Equipment",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_TenantId_ProductionLineId",
                table: "Equipment",
                columns: new[] { "TenantId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeRecords_TenantId_EquipmentId",
                table: "DowntimeRecords",
                columns: new[] { "TenantId", "EquipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_TenantId_DocumentId_IsActive",
                table: "DocumentVersions",
                columns: new[] { "TenantId", "DocumentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_TenantId_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "TenantId", "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_FileName",
                table: "Documents",
                columns: new[] { "TenantId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_TenantId_DocumentId_Sequence",
                table: "DocumentChunks",
                columns: new[] { "TenantId", "DocumentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_TenantId_Code",
                table: "DefectTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectRecords_TenantId_DefectTypeId",
                table: "DefectRecords",
                columns: new[] { "TenantId", "DefectTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefectRecords_TenantId_QualityInspectionId",
                table: "DefectRecords",
                columns: new[] { "TenantId", "QualityInspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_UpdatedAt",
                table: "Conversations",
                columns: new[] { "TenantId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_UserId",
                table: "Conversations",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_TenantId_ConversationId_CreatedAt",
                table: "ConversationMessages",
                columns: new[] { "TenantId", "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Boms_TenantId_Code",
                table: "Boms",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boms_TenantId_ProductId",
                table: "Boms",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_TenantId_BomId",
                table: "BomItems",
                columns: new[] { "TenantId", "BomId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_TenantId_MaterialId",
                table: "BomItems",
                columns: new[] { "TenantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantMemberships_TenantId",
                table: "UserTenantMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantMemberships_UserId_TenantId",
                table: "UserTenantMemberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Boms_TenantId_BomId",
                table: "BomItems",
                columns: new[] { "TenantId", "BomId" },
                principalTable: "Boms",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems",
                columns: new[] { "TenantId", "MaterialId" },
                principalTable: "Materials",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Tenants_TenantId",
                table: "BomItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boms_Products_TenantId_ProductId",
                table: "Boms",
                columns: new[] { "TenantId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Boms_Tenants_TenantId",
                table: "Boms",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_Conversations_TenantId_ConversationId",
                table: "ConversationMessages",
                columns: new[] { "TenantId", "ConversationId" },
                principalTable: "Conversations",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_Tenants_TenantId",
                table: "ConversationMessages",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Tenants_TenantId",
                table: "Conversations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectRecords_DefectTypes_TenantId_DefectTypeId",
                table: "DefectRecords",
                columns: new[] { "TenantId", "DefectTypeId" },
                principalTable: "DefectTypes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectRecords_QualityInspections_TenantId_QualityInspection~",
                table: "DefectRecords",
                columns: new[] { "TenantId", "QualityInspectionId" },
                principalTable: "QualityInspections",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectRecords_Tenants_TenantId",
                table: "DefectRecords",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectTypes_Tenants_TenantId",
                table: "DefectTypes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_Documents_TenantId_DocumentId",
                table: "DocumentChunks",
                columns: new[] { "TenantId", "DocumentId" },
                principalTable: "Documents",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_Tenants_TenantId",
                table: "DocumentChunks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Tenants_TenantId",
                table: "Documents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Documents_TenantId_DocumentId",
                table: "DocumentVersions",
                columns: new[] { "TenantId", "DocumentId" },
                principalTable: "Documents",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Tenants_TenantId",
                table: "DocumentVersions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DowntimeRecords_Equipment_TenantId_EquipmentId",
                table: "DowntimeRecords",
                columns: new[] { "TenantId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DowntimeRecords_Tenants_TenantId",
                table: "DowntimeRecords",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_ProductionLines_TenantId_ProductionLineId",
                table: "Equipment",
                columns: new[] { "TenantId", "ProductionLineId" },
                principalTable: "ProductionLines",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Tenants_TenantId",
                table: "Equipment",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentAlarms_Equipment_TenantId_EquipmentId",
                table: "EquipmentAlarms",
                columns: new[] { "TenantId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentAlarms_Tenants_TenantId",
                table: "EquipmentAlarms",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStatuses_Equipment_TenantId_EquipmentId",
                table: "EquipmentStatuses",
                columns: new[] { "TenantId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStatuses_Tenants_TenantId",
                table: "EquipmentStatuses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Tenants_TenantId",
                table: "Materials",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRoutes_Products_TenantId_ProductId",
                table: "ProcessRoutes",
                columns: new[] { "TenantId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRoutes_Tenants_TenantId",
                table: "ProcessRoutes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessSteps_ProcessRoutes_TenantId_ProcessRouteId",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "ProcessRouteId" },
                principalTable: "ProcessRoutes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessSteps_Tenants_TenantId",
                table: "ProcessSteps",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessSteps_Workstations_TenantId_WorkstationId",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "WorkstationId" },
                principalTable: "Workstations",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionLines_Tenants_TenantId",
                table: "ProductionLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_Equipment_TenantId_EquipmentId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_ProcessSteps_TenantId_ProcessStepId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "ProcessStepId" },
                principalTable: "ProcessSteps",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_Tenants_TenantId",
                table: "ProductionReports",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_WorkOrders_TenantId_WorkOrderId",
                table: "ProductionReports",
                columns: new[] { "TenantId", "WorkOrderId" },
                principalTable: "WorkOrders",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_ProcessSteps_TenantId_ProcessStepId",
                table: "QualityInspections",
                columns: new[] { "TenantId", "ProcessStepId" },
                principalTable: "ProcessSteps",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_Tenants_TenantId",
                table: "QualityInspections",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_WorkOrders_TenantId_WorkOrderId",
                table: "QualityInspections",
                columns: new[] { "TenantId", "WorkOrderId" },
                principalTable: "WorkOrders",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_ProcessSteps_TenantId_ProcessStepId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "ProcessStepId" },
                principalTable: "ProcessSteps",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_Tenants_TenantId",
                table: "WorkOrderOperations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_WorkOrders_TenantId_WorkOrderId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "WorkOrderId" },
                principalTable: "WorkOrders",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_ProductionLines_TenantId_ProductionLineId",
                table: "WorkOrders",
                columns: new[] { "TenantId", "ProductionLineId" },
                principalTable: "ProductionLines",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Products_TenantId_ProductId",
                table: "WorkOrders",
                columns: new[] { "TenantId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Tenants_TenantId",
                table: "WorkOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workstations_ProductionLines_TenantId_ProductionLineId",
                table: "Workstations",
                columns: new[] { "TenantId", "ProductionLineId" },
                principalTable: "ProductionLines",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workstations_Tenants_TenantId",
                table: "Workstations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Boms_TenantId_BomId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Materials_TenantId_MaterialId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BomItems_Tenants_TenantId",
                table: "BomItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Boms_Products_TenantId_ProductId",
                table: "Boms");

            migrationBuilder.DropForeignKey(
                name: "FK_Boms_Tenants_TenantId",
                table: "Boms");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_Conversations_TenantId_ConversationId",
                table: "ConversationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_Tenants_TenantId",
                table: "ConversationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Tenants_TenantId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectRecords_DefectTypes_TenantId_DefectTypeId",
                table: "DefectRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectRecords_QualityInspections_TenantId_QualityInspection~",
                table: "DefectRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectRecords_Tenants_TenantId",
                table: "DefectRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectTypes_Tenants_TenantId",
                table: "DefectTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_Documents_TenantId_DocumentId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_Tenants_TenantId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Tenants_TenantId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_TenantId_DocumentId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Tenants_TenantId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DowntimeRecords_Equipment_TenantId_EquipmentId",
                table: "DowntimeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DowntimeRecords_Tenants_TenantId",
                table: "DowntimeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_ProductionLines_TenantId_ProductionLineId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Tenants_TenantId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentAlarms_Equipment_TenantId_EquipmentId",
                table: "EquipmentAlarms");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentAlarms_Tenants_TenantId",
                table: "EquipmentAlarms");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStatuses_Equipment_TenantId_EquipmentId",
                table: "EquipmentStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStatuses_Tenants_TenantId",
                table: "EquipmentStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Tenants_TenantId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRoutes_Products_TenantId_ProductId",
                table: "ProcessRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRoutes_Tenants_TenantId",
                table: "ProcessRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessSteps_ProcessRoutes_TenantId_ProcessRouteId",
                table: "ProcessSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessSteps_Tenants_TenantId",
                table: "ProcessSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessSteps_Workstations_TenantId_WorkstationId",
                table: "ProcessSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionLines_Tenants_TenantId",
                table: "ProductionLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_Equipment_TenantId_EquipmentId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_ProcessSteps_TenantId_ProcessStepId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_Tenants_TenantId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionReports_WorkOrders_TenantId_WorkOrderId",
                table: "ProductionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_ProcessSteps_TenantId_ProcessStepId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_Tenants_TenantId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityInspections_WorkOrders_TenantId_WorkOrderId",
                table: "QualityInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_ProcessSteps_TenantId_ProcessStepId",
                table: "WorkOrderOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_Tenants_TenantId",
                table: "WorkOrderOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_WorkOrders_TenantId_WorkOrderId",
                table: "WorkOrderOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_ProductionLines_TenantId_ProductionLineId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Products_TenantId_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Tenants_TenantId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Workstations_ProductionLines_TenantId_ProductionLineId",
                table: "Workstations");

            migrationBuilder.DropForeignKey(
                name: "FK_Workstations_Tenants_TenantId",
                table: "Workstations");

            migrationBuilder.DropTable(
                name: "UserTenantMemberships");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Workstations_TenantId_Id",
                table: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_Workstations_TenantId_Code",
                table: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_Workstations_TenantId_ProductionLineId",
                table: "Workstations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkOrders_TenantId_Id",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_Code",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_CreatedAt",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_PlannedEndTime",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_PlannedStartTime",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_ProductId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_ProductionLineId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenantId_Status",
                table: "WorkOrders");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkOrderOperations_TenantId_Id",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_TenantId_ProcessStepId",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId",
                table: "WorkOrderOperations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_QualityInspections_TenantId_Id",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_TenantId_BatchNumber",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_TenantId_Code",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_TenantId_InspectionTime",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_TenantId_ProcessStepId",
                table: "QualityInspections");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_TenantId_WorkOrderId",
                table: "QualityInspections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Code",
                table: "Products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProductionReports_TenantId_Id",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_TenantId_BatchNumber",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_TenantId_EquipmentId",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_TenantId_ProcessStepId",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_TenantId_Timestamp",
                table: "ProductionReports");

            migrationBuilder.DropIndex(
                name: "IX_ProductionReports_TenantId_WorkOrderId",
                table: "ProductionReports");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProductionLines_TenantId_Id",
                table: "ProductionLines");

            migrationBuilder.DropIndex(
                name: "IX_ProductionLines_TenantId_Code",
                table: "ProductionLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProcessSteps_TenantId_Id",
                table: "ProcessSteps");

            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId",
                table: "ProcessSteps");

            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_TenantId_WorkstationId",
                table: "ProcessSteps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProcessRoutes_TenantId_Id",
                table: "ProcessRoutes");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRoutes_TenantId_Code",
                table: "ProcessRoutes");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRoutes_TenantId_ProductId",
                table: "ProcessRoutes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Materials_TenantId_Id",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_TenantId_Code",
                table: "Materials");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EquipmentStatuses_TenantId_Id",
                table: "EquipmentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentStatuses_TenantId_EquipmentId_StartTime",
                table: "EquipmentStatuses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EquipmentAlarms_TenantId_Id",
                table: "EquipmentAlarms");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentAlarms_TenantId_EquipmentId_OccurredAt",
                table: "EquipmentAlarms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Equipment_TenantId_Id",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_TenantId_Code",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_TenantId_ProductionLineId",
                table: "Equipment");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DowntimeRecords_TenantId_Id",
                table: "DowntimeRecords");

            migrationBuilder.DropIndex(
                name: "IX_DowntimeRecords_TenantId_EquipmentId",
                table: "DowntimeRecords");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DocumentVersions_TenantId_Id",
                table: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_TenantId_DocumentId_IsActive",
                table: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_TenantId_DocumentId_VersionNumber",
                table: "DocumentVersions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Documents_TenantId_Id",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_FileName",
                table: "Documents");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DocumentChunks_TenantId_Id",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_TenantId_DocumentId_Sequence",
                table: "DocumentChunks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DefectTypes_TenantId_Id",
                table: "DefectTypes");

            migrationBuilder.DropIndex(
                name: "IX_DefectTypes_TenantId_Code",
                table: "DefectTypes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DefectRecords_TenantId_Id",
                table: "DefectRecords");

            migrationBuilder.DropIndex(
                name: "IX_DefectRecords_TenantId_DefectTypeId",
                table: "DefectRecords");

            migrationBuilder.DropIndex(
                name: "IX_DefectRecords_TenantId_QualityInspectionId",
                table: "DefectRecords");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Conversations_TenantId_Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_UpdatedAt",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_UserId",
                table: "Conversations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ConversationMessages_TenantId_Id",
                table: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_TenantId_ConversationId_CreatedAt",
                table: "ConversationMessages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Boms_TenantId_Id",
                table: "Boms");

            migrationBuilder.DropIndex(
                name: "IX_Boms_TenantId_Code",
                table: "Boms");

            migrationBuilder.DropIndex(
                name: "IX_Boms_TenantId_ProductId",
                table: "Boms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_BomItems_TenantId_Id",
                table: "BomItems");

            migrationBuilder.DropIndex(
                name: "IX_BomItems_TenantId_BomId",
                table: "BomItems");

            migrationBuilder.DropIndex(
                name: "IX_BomItems_TenantId_MaterialId",
                table: "BomItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Workstations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkOrderOperations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductionReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductionLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProcessSteps");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProcessRoutes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EquipmentStatuses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EquipmentAlarms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DowntimeRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DefectTypes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DefectRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Boms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BomItems");

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_Code",
                table: "Workstations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_ProductionLineId",
                table: "Workstations",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Code",
                table: "WorkOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedAt",
                table: "WorkOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PlannedEndTime",
                table: "WorkOrders",
                column: "PlannedEndTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PlannedStartTime",
                table: "WorkOrders",
                column: "PlannedStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductId",
                table: "WorkOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductionLineId",
                table: "WorkOrders",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Status",
                table: "WorkOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_ProcessStepId",
                table: "WorkOrderOperations",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_WorkOrderId",
                table: "WorkOrderOperations",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_BatchNumber",
                table: "QualityInspections",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_Code",
                table: "QualityInspections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionTime",
                table: "QualityInspections",
                column: "InspectionTime");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProcessStepId",
                table: "QualityInspections",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_WorkOrderId",
                table: "QualityInspections",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_BatchNumber",
                table: "ProductionReports",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_ProcessStepId",
                table: "ProductionReports",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_Timestamp",
                table: "ProductionReports",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionReports_WorkOrderId",
                table: "ProductionReports",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_Code",
                table: "ProductionLines",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_ProcessRouteId",
                table: "ProcessSteps",
                column: "ProcessRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_WorkstationId",
                table: "ProcessSteps",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRoutes_Code",
                table: "ProcessRoutes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRoutes_ProductId",
                table: "ProcessRoutes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Code",
                table: "Materials",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentStatuses_EquipmentId_StartTime",
                table: "EquipmentStatuses",
                columns: new[] { "EquipmentId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAlarms_EquipmentId_OccurredAt",
                table: "EquipmentAlarms",
                columns: new[] { "EquipmentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Code",
                table: "Equipment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_ProductionLineId",
                table: "Equipment",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeRecords_EquipmentId",
                table: "DowntimeRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_IsActive",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileName",
                table: "Documents",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId_Sequence",
                table: "DocumentChunks",
                columns: new[] { "DocumentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_Code",
                table: "DefectTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectRecords_DefectTypeId",
                table: "DefectRecords",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectRecords_QualityInspectionId",
                table: "DefectRecords",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UpdatedAt",
                table: "Conversations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAt",
                table: "ConversationMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Boms_Code",
                table: "Boms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boms_ProductId",
                table: "Boms",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_BomId",
                table: "BomItems",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_MaterialId",
                table: "BomItems",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Boms_BomId",
                table: "BomItems",
                column: "BomId",
                principalTable: "Boms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BomItems_Materials_MaterialId",
                table: "BomItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Boms_Products_ProductId",
                table: "Boms",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_Conversations_ConversationId",
                table: "ConversationMessages",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectRecords_DefectTypes_DefectTypeId",
                table: "DefectRecords",
                column: "DefectTypeId",
                principalTable: "DefectTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectRecords_QualityInspections_QualityInspectionId",
                table: "DefectRecords",
                column: "QualityInspectionId",
                principalTable: "QualityInspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DowntimeRecords_Equipment_EquipmentId",
                table: "DowntimeRecords",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_ProductionLines_ProductionLineId",
                table: "Equipment",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentAlarms_Equipment_EquipmentId",
                table: "EquipmentAlarms",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStatuses_Equipment_EquipmentId",
                table: "EquipmentStatuses",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRoutes_Products_ProductId",
                table: "ProcessRoutes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessSteps_ProcessRoutes_ProcessRouteId",
                table: "ProcessSteps",
                column: "ProcessRouteId",
                principalTable: "ProcessRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessSteps_Workstations_WorkstationId",
                table: "ProcessSteps",
                column: "WorkstationId",
                principalTable: "Workstations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_ProcessSteps_ProcessStepId",
                table: "ProductionReports",
                column: "ProcessStepId",
                principalTable: "ProcessSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionReports_WorkOrders_WorkOrderId",
                table: "ProductionReports",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_ProcessSteps_ProcessStepId",
                table: "QualityInspections",
                column: "ProcessStepId",
                principalTable: "ProcessSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityInspections_WorkOrders_WorkOrderId",
                table: "QualityInspections",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_ProcessSteps_ProcessStepId",
                table: "WorkOrderOperations",
                column: "ProcessStepId",
                principalTable: "ProcessSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_WorkOrders_WorkOrderId",
                table: "WorkOrderOperations",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_ProductionLines_ProductionLineId",
                table: "WorkOrders",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Products_ProductId",
                table: "WorkOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workstations_ProductionLines_ProductionLineId",
                table: "Workstations",
                column: "ProductionLineId",
                principalTable: "ProductionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
