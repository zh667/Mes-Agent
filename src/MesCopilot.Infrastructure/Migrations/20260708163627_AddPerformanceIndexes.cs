using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedAt",
                table: "WorkOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PlannedEndTime",
                table: "WorkOrders",
                column: "PlannedEndTime");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionTime",
                table: "QualityInspections",
                column: "InspectionTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_CreatedAt",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_PlannedEndTime",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_QualityInspections_InspectionTime",
                table: "QualityInspections");
        }
    }
}
