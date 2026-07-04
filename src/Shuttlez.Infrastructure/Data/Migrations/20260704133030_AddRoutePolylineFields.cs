using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddRoutePolylineFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "BoundsMaxLatitude",
            table: "RoutesSet",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "BoundsMaxLongitude",
            table: "RoutesSet",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "BoundsMinLatitude",
            table: "RoutesSet",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "BoundsMinLongitude",
            table: "RoutesSet",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DistanceMeters",
            table: "RoutesSet",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DurationSeconds",
            table: "RoutesSet",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EncodedPolyline",
            table: "RoutesSet",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BoundsMaxLatitude",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "BoundsMaxLongitude",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "BoundsMinLatitude",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "BoundsMinLongitude",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "DistanceMeters",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "DurationSeconds",
            table: "RoutesSet");

        migrationBuilder.DropColumn(
            name: "EncodedPolyline",
            table: "RoutesSet");
    }
}
