using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripIdToSupportTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TripId",
                table: "SupportTicketsSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketsSet_TripId",
                table: "SupportTicketsSet",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportTicketsSet_TripsSet_TripId",
                table: "SupportTicketsSet",
                column: "TripId",
                principalTable: "TripsSet",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportTicketsSet_TripsSet_TripId",
                table: "SupportTicketsSet");

            migrationBuilder.DropIndex(
                name: "IX_SupportTicketsSet_TripId",
                table: "SupportTicketsSet");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "SupportTicketsSet");
        }
    }
}
