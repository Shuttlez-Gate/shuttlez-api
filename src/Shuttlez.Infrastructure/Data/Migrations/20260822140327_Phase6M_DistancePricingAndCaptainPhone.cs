using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6M_DistancePricingAndCaptainPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseFareApplied",
                table: "RideRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "RideRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumFareApplied",
                table: "RideRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKmApplied",
                table: "RideRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFare",
                table: "RideFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumFare",
                table: "RideFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumFare",
                table: "RideFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKm",
                table: "RideFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFareApplied",
                table: "GroupRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "GroupRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumFareApplied",
                table: "GroupRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKmApplied",
                table: "GroupRequestsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFare",
                table: "GroupFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumFare",
                table: "GroupFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumFare",
                table: "GroupFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKm",
                table: "GroupFareRulesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseFareApplied",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "MinimumFareApplied",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "PricePerKmApplied",
                table: "RideRequestsSet");

            migrationBuilder.DropColumn(
                name: "BaseFare",
                table: "RideFareRulesSet");

            migrationBuilder.DropColumn(
                name: "MaximumFare",
                table: "RideFareRulesSet");

            migrationBuilder.DropColumn(
                name: "MinimumFare",
                table: "RideFareRulesSet");

            migrationBuilder.DropColumn(
                name: "PricePerKm",
                table: "RideFareRulesSet");

            migrationBuilder.DropColumn(
                name: "BaseFareApplied",
                table: "GroupRequestsSet");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "GroupRequestsSet");

            migrationBuilder.DropColumn(
                name: "MinimumFareApplied",
                table: "GroupRequestsSet");

            migrationBuilder.DropColumn(
                name: "PricePerKmApplied",
                table: "GroupRequestsSet");

            migrationBuilder.DropColumn(
                name: "BaseFare",
                table: "GroupFareRulesSet");

            migrationBuilder.DropColumn(
                name: "MaximumFare",
                table: "GroupFareRulesSet");

            migrationBuilder.DropColumn(
                name: "MinimumFare",
                table: "GroupFareRulesSet");

            migrationBuilder.DropColumn(
                name: "PricePerKm",
                table: "GroupFareRulesSet");
        }
    }
}
