using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class ApplyRoutePolylineSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "EncodedPolyline" text;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "DistanceMeters" integer;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "DurationSeconds" integer;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "BoundsMinLatitude" double precision;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "BoundsMaxLatitude" double precision;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "BoundsMinLongitude" double precision;
            ALTER TABLE "RoutesSet" ADD COLUMN IF NOT EXISTS "BoundsMaxLongitude" double precision;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "BoundsMaxLongitude";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "BoundsMinLongitude";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "BoundsMaxLatitude";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "BoundsMinLatitude";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "DurationSeconds";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "DistanceMeters";
            ALTER TABLE "RoutesSet" DROP COLUMN IF EXISTS "EncodedPolyline";
            """);
    }
}
