using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPgVectorSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.Sql("""
                ALTER TABLE "DocumentChunks"
                ALTER COLUMN "Vector" TYPE vector(1536)
                USING "Vector"::vector;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.Sql("""
                ALTER TABLE "DocumentChunks"
                ALTER COLUMN "Vector" TYPE text
                USING "Vector"::text;
                """);
        }
    }
}
