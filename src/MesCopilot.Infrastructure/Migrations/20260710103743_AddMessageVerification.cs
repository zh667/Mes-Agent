using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationJson",
                table: "ConversationMessages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationSchemaVersion",
                table: "ConversationMessages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationJson",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "VerificationSchemaVersion",
                table: "ConversationMessages");
        }
    }
}
