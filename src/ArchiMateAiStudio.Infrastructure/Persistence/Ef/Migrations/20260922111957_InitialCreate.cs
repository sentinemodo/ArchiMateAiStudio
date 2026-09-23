using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ArchimateXml = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatchJson = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_models_UpdatedAt",
                table: "models",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_CreatedAt",
                table: "proposals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_ModelId",
                table: "proposals",
                column: "ModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "models");

            migrationBuilder.DropTable(
                name: "proposals");
        }
    }
}
