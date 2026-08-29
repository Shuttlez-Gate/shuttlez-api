using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocumentEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentEn",
                table: "LegalDocumentsSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "LegalDocumentsSet",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentEn",
                table: "LegalDocumentsSet");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "LegalDocumentsSet");
        }
    }
}
