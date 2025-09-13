using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DbMaker.Shared.Data;

#nullable disable

namespace DbMaker.API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbMakerDbContext))]
    [Migration("20250913170000_AddTemplateLibrary")]
    public partial class AddTemplateLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LatestVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DockerImage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ConnectionStringTemplate = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ports = table.Column<string>(type: "TEXT", nullable: false),
                    Volumes = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultEnvironment = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultConfiguration = table.Column<string>(type: "TEXT", nullable: false),
                    Healthcheck = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Key",
                table: "Templates",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId_Version",
                table: "TemplateVersions",
                columns: new[] { "TemplateId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateVersions");

            migrationBuilder.DropTable(
                name: "Templates");
        }
    }
}
