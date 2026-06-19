using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publisher.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupportedLanguagesJson",
                table: "Sites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalizedColumnsJson",
                table: "SiteFieldMappings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Posts",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<Guid>(
                name: "TranslationGroupId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TranslationGroupId",
                table: "Posts",
                column: "TranslationGroupId",
                filter: "[TranslationGroupId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_TranslationGroupId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SupportedLanguagesJson",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LocalizedColumnsJson",
                table: "SiteFieldMappings");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TranslationGroupId",
                table: "Posts");
        }
    }
}
