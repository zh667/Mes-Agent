using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceProcessStepSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId",
                table: "ProcessSteps");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId_Sequence",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "ProcessRouteId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId_Sequence",
                table: "ProcessSteps");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessSteps_TenantId_ProcessRouteId",
                table: "ProcessSteps",
                columns: new[] { "TenantId", "ProcessRouteId" });
        }
    }
}
