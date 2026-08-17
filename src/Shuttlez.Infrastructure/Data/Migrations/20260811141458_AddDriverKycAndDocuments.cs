using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverKycAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "DriversSet",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenseExpiry",
                table: "DriversSet",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseType",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufactureYear",
                table: "DriversSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlateNumber",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seats",
                table: "DriversSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleColor",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleKind",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleModelName",
                table: "DriversSet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "DriversSet",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "DriversSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverDocumentsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDocumentsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDocumentsSet_DriversSet_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriversSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocumentsSet_DriverId",
                table: "DriverDocumentsSet",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverDocumentsSet");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "LicenseExpiry",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "ManufactureYear",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "PlateNumber",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "Seats",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "VehicleColor",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "VehicleKind",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "VehicleModelName",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "DriversSet");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "DriversSet");
        }
    }
}
