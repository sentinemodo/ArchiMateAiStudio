using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddRagChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prefer pgvector when available (Neon); do not fail MVP migrate if extension is blocked.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  CREATE EXTENSION IF NOT EXISTS vector;
                EXCEPTION
                  WHEN OTHERS THEN
                    RAISE NOTICE 'pgvector extension unavailable: %', SQLERRM;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "rag_chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_chunks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rag_chunks_ModelId",
                table: "rag_chunks",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_chunks_ModelId_Kind_SourceId",
                table: "rag_chunks",
                columns: new[] { "ModelId", "Kind", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rag_chunks");
        }
    }
}
