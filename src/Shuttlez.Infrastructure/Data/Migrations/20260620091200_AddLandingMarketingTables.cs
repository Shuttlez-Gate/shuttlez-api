using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingMarketingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandingCaptainLeadsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    VehicleType = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingCaptainLeadsSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LandingRouteLeadsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FromCity = table.Column<string>(type: "text", nullable: false),
                    FromRegion = table.Column<string>(type: "text", nullable: false),
                    FromTime = table.Column<string>(type: "text", nullable: true),
                    ToCity = table.Column<string>(type: "text", nullable: false),
                    ToRegion = table.Column<string>(type: "text", nullable: false),
                    ToTime = table.Column<string>(type: "text", nullable: true),
                    WeeklyCount = table.Column<int>(type: "integer", nullable: false),
                    UsageDays = table.Column<string>(type: "text", nullable: true),
                    UsageReason = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingRouteLeadsSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LandingWaitlistEntriesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingWaitlistEntriesSet", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LandingCaptainLeadsSet");

            migrationBuilder.DropTable(
                name: "LandingRouteLeadsSet");

            migrationBuilder.DropTable(
                name: "LandingWaitlistEntriesSet");
        }
    }
}
