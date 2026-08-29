using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAuthProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookProviderId",
                table: "UsersSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleProviderId",
                table: "UsersSet",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersSet_FacebookProviderId",
                table: "UsersSet",
                column: "FacebookProviderId",
                unique: true,
                filter: "\"FacebookProviderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsersSet_GoogleProviderId",
                table: "UsersSet",
                column: "GoogleProviderId",
                unique: true,
                filter: "\"GoogleProviderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsersSet_FacebookProviderId",
                table: "UsersSet");

            migrationBuilder.DropIndex(
                name: "IX_UsersSet_GoogleProviderId",
                table: "UsersSet");

            migrationBuilder.DropColumn(
                name: "FacebookProviderId",
                table: "UsersSet");

            migrationBuilder.DropColumn(
                name: "GoogleProviderId",
                table: "UsersSet");
        }
    }
}
