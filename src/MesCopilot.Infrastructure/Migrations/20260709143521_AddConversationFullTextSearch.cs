using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_ConversationMessages_Content_FullText"
                ON "ConversationMessages"
                USING GIN (to_tsvector('simple', "Content"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_ConversationMessages_Content_FullText";
                """);
        }
    }
}
