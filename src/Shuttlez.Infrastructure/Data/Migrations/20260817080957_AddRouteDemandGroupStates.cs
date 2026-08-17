using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteDemandGroupStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteDemandGroupStatesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteKey = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedDriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteDemandGroupStatesSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteDemandGroupStatesSet_DriversSet_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "DriversSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteDemandGroupStatesSet_AssignedDriverId",
                table: "RouteDemandGroupStatesSet",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteDemandGroupStatesSet_RouteKey",
                table: "RouteDemandGroupStatesSet",
                column: "RouteKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteDemandGroupStatesSet");
        }
    }
}
