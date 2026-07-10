using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId",
                table: "WorkOrderOperations");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "WorkOrderOperations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "WorkOrderOperations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "WorkstationId",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_TenantId_EquipmentId_PlannedStartTime_P~",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "EquipmentId", "PlannedStartTime", "PlannedEndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId_ProcessStepId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "WorkOrderId", "ProcessStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_TenantId_WorkstationId",
                table: "Equipment",
                columns: new[] { "TenantId", "WorkstationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Workstations_TenantId_WorkstationId",
                table: "Equipment",
                columns: new[] { "TenantId", "WorkstationId" },
                principalTable: "Workstations",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderOperations_Equipment_TenantId_EquipmentId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "EquipmentId" },
                principalTable: "Equipment",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Workstations_TenantId_WorkstationId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderOperations_Equipment_TenantId_EquipmentId",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_TenantId_EquipmentId_PlannedStartTime_P~",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId_ProcessStepId",
                table: "WorkOrderOperations");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_TenantId_WorkstationId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "WorkOrderOperations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "WorkOrderOperations");

            migrationBuilder.DropColumn(
                name: "WorkstationId",
                table: "Equipment");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_TenantId_WorkOrderId",
                table: "WorkOrderOperations",
                columns: new[] { "TenantId", "WorkOrderId" });
        }
    }
}
