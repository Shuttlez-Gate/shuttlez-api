using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PhasePricingRulesAndEarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingRulesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    OneWayPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RoundTripPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WeeklyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LaunchCommissionPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    PermanentCommissionPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    LaunchPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    LaunchStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinimumLaunchRiders = table.Column<int>(type: "integer", nullable: false),
                    TargetOccupancy = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRulesSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRulesSet_RoutesSet_RouteId",
                        column: x => x.RouteId,
                        principalTable: "RoutesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRulesSet_EffectiveFrom",
                table: "PricingRulesSet",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRulesSet_EffectiveTo",
                table: "PricingRulesSet",
                column: "EffectiveTo");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRulesSet_RouteId_VehicleType_IsActive",
                table: "PricingRulesSet",
                columns: new[] { "RouteId", "VehicleType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingRulesSet");
        }
    }
}
