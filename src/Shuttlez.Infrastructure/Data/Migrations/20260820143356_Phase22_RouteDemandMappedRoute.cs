using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase22_RouteDemandMappedRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MappedAt",
                table: "RouteDemandGroupStatesSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MappedByUserId",
                table: "RouteDemandGroupStatesSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MappedRouteId",
                table: "RouteDemandGroupStatesSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteDemandGroupStatesSet_MappedRouteId",
                table: "RouteDemandGroupStatesSet",
                column: "MappedRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteDemandGroupStatesSet_RoutesSet_MappedRouteId",
                table: "RouteDemandGroupStatesSet",
                column: "MappedRouteId",
                principalTable: "RoutesSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteDemandGroupStatesSet_RoutesSet_MappedRouteId",
                table: "RouteDemandGroupStatesSet");

            migrationBuilder.DropIndex(
                name: "IX_RouteDemandGroupStatesSet_MappedRouteId",
                table: "RouteDemandGroupStatesSet");

            migrationBuilder.DropColumn(
                name: "MappedAt",
                table: "RouteDemandGroupStatesSet");

            migrationBuilder.DropColumn(
                name: "MappedByUserId",
                table: "RouteDemandGroupStatesSet");

            migrationBuilder.DropColumn(
                name: "MappedRouteId",
                table: "RouteDemandGroupStatesSet");
        }
    }
}
