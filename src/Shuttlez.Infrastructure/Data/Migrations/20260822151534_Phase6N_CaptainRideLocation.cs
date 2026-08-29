using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6N_CaptainRideLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CaptainLatitude",
                table: "RideRequestsSet",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CaptainLocationUpdatedAt",
                table: "RideRequestsSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CaptainLongitude",
                table: "RideRequestsSet",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptainLatitude",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "CaptainLocationUpdatedAt",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "CaptainLongitude",
                table: "RideRequestsSet");
        }
    }
}
