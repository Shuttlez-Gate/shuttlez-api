using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ShuttlePricingCommissionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionActivatedAt",
                table: "UsersSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerSeat",
                table: "TripsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InvoicesSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "BookingsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "CaptainEarnings",
                table: "BookingsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                table: "BookingsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "BookingsSet",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerSeat",
                table: "BookingsSet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UsesSubscriptionCredit",
                table: "BookingsSet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CommissionRulesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlatformCommissionPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRulesSet", x => x.Id);
                });

            // Pre-Phase-3 bookings: launch default = 0% platform → captain gets full fare.
            migrationBuilder.Sql(
                """
                UPDATE "BookingsSet"
                SET
                    "CaptainEarnings" = "TotalAmount",
                    "PricePerSeat" = CASE
                        WHEN "SeatCount" > 0 THEN ROUND(("TotalAmount" / "SeatCount")::numeric, 2)
                        ELSE 0
                    END,
                    "CommissionRate" = 0,
                    "CommissionAmount" = 0
                WHERE "PricePerSeat" = 0
                  AND "CommissionAmount" = 0
                  AND "TotalAmount" > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionRulesSet");

            migrationBuilder.DropColumn(
                name: "SubscriptionActivatedAt",
                table: "UsersSet");

            migrationBuilder.DropColumn(
                name: "CaptainEarnings",
                table: "BookingsSet");

            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                table: "BookingsSet");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "BookingsSet");

            migrationBuilder.DropColumn(
                name: "PricePerSeat",
                table: "BookingsSet");

            migrationBuilder.DropColumn(
                name: "UsesSubscriptionCredit",
                table: "BookingsSet");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerSeat",
                table: "TripsSet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InvoicesSet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "BookingsSet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
