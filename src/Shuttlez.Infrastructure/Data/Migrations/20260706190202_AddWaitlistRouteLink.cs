using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistRouteLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteFrom",
                table: "LandingWaitlistEntriesSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId",
                table: "LandingWaitlistEntriesSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteTo",
                table: "LandingWaitlistEntriesSet",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandingWaitlistEntriesSet_RouteId",
                table: "LandingWaitlistEntriesSet",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_LandingWaitlistEntriesSet_RoutesSet_RouteId",
                table: "LandingWaitlistEntriesSet",
                column: "RouteId",
                principalTable: "RoutesSet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LandingWaitlistEntriesSet_RoutesSet_RouteId",
                table: "LandingWaitlistEntriesSet");

            migrationBuilder.DropIndex(
                name: "IX_LandingWaitlistEntriesSet_RouteId",
                table: "LandingWaitlistEntriesSet");

            migrationBuilder.DropColumn(
                name: "RouteFrom",
                table: "LandingWaitlistEntriesSet");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "LandingWaitlistEntriesSet");

            migrationBuilder.DropColumn(
                name: "RouteTo",
                table: "LandingWaitlistEntriesSet");
        }
    }
}
