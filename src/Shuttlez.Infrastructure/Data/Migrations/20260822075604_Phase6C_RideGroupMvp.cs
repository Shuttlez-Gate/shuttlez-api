using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6C_RideGroupMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupFareRulesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FromZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CharterFlatFare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupFareRulesSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideFareRulesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FromZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FlatFare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideFareRulesSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupRequestsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupLatitude = table.Column<double>(type: "double precision", nullable: false),
                    PickupLongitude = table.Column<double>(type: "double precision", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DestinationLatitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationLongitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FromZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GroupFareRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    JoinedMemberCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FareAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CaptainEarnings = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsCashConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    MembershipLocked = table.Column<bool>(type: "boolean", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupRequestsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupRequestsSet_DriversSet_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriversSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GroupRequestsSet_GroupFareRulesSet_GroupFareRuleId",
                        column: x => x.GroupFareRuleId,
                        principalTable: "GroupFareRulesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GroupRequestsSet_UsersSet_OrganizerUserId",
                        column: x => x.OrganizerUserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RideRequestsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupLatitude = table.Column<double>(type: "double precision", nullable: false),
                    PickupLongitude = table.Column<double>(type: "double precision", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DestinationLatitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationLongitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FromZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToZoneKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RideFareRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FareAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CaptainEarnings = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsCashConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideRequestsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideRequestsSet_DriversSet_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriversSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RideRequestsSet_RideFareRulesSet_RideFareRuleId",
                        column: x => x.RideFareRuleId,
                        principalTable: "RideFareRulesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RideRequestsSet_UsersSet_RiderUserId",
                        column: x => x.RiderUserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembersSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOrganizer = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembersSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembersSet_GroupRequestsSet_GroupRequestId",
                        column: x => x.GroupRequestId,
                        principalTable: "GroupRequestsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembersSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupFareRulesSet_FromZoneKey_ToZoneKey_IsActive",
                table: "GroupFareRulesSet",
                columns: new[] { "FromZoneKey", "ToZoneKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembersSet_GroupRequestId_UserId",
                table: "GroupMembersSet",
                columns: new[] { "GroupRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembersSet_UserId",
                table: "GroupMembersSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequestsSet_DriverId",
                table: "GroupRequestsSet",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequestsSet_GroupFareRuleId",
                table: "GroupRequestsSet",
                column: "GroupFareRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequestsSet_OrganizerUserId",
                table: "GroupRequestsSet",
                column: "OrganizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRequestsSet_Status",
                table: "GroupRequestsSet",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RideFareRulesSet_FromZoneKey_ToZoneKey_IsActive",
                table: "RideFareRulesSet",
                columns: new[] { "FromZoneKey", "ToZoneKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RideRequestsSet_DriverId",
                table: "RideRequestsSet",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequestsSet_RideFareRuleId",
                table: "RideRequestsSet",
                column: "RideFareRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequestsSet_RiderUserId",
                table: "RideRequestsSet",
                column: "RiderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequestsSet_Status",
                table: "RideRequestsSet",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMembersSet");

            migrationBuilder.DropTable(
                name: "RideRequestsSet");

            migrationBuilder.DropTable(
                name: "GroupRequestsSet");

            migrationBuilder.DropTable(
                name: "RideFareRulesSet");

            migrationBuilder.DropTable(
                name: "GroupFareRulesSet");
        }
    }
}
