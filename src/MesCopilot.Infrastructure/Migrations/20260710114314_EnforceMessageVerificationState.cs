using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceMessageVerificationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ConversationMessages_VerificationState",
                table: "ConversationMessages",
                sql: "(\"VerificationJson\" IS NULL AND \"VerificationSchemaVersion\" IS NULL) OR " +
                    "(\"VerificationJson\" IS NOT NULL AND \"VerificationSchemaVersion\" = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ConversationMessages_VerificationState",
                table: "ConversationMessages");
        }
    }
}
