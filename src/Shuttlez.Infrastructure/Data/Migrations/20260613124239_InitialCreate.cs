using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttlez.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaqItemsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItemsSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentsSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpRequestsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRequestsSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: false),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: false),
                    EndLatitude = table.Column<double>(type: "double precision", nullable: false),
                    EndLongitude = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutesSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPackagesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    TripCount = table.Column<int>(type: "integer", nullable: false),
                    ValidityDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPackagesSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsersSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    RatingAverage = table.Column<decimal>(type: "numeric", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    FcmToken = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehiclesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlateNumber = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclesSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StopsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopsSet_RoutesSet_RouteId",
                        column: x => x.RouteId,
                        principalTable: "RoutesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokensSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokensSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokensSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stars = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteRequestsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    FromLatitude = table.Column<double>(type: "double precision", nullable: false),
                    FromLongitude = table.Column<double>(type: "double precision", nullable: false),
                    ToLatitude = table.Column<double>(type: "double precision", nullable: false),
                    ToLongitude = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteRequestsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteRequestsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedLocationsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLocationsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedLocationsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriversSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RatingAverage = table.Column<decimal>(type: "numeric", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriversSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriversSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriversSet_VehiclesSet_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VehiclesSet",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportMessagesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFromSupport = table.Column<bool>(type: "boolean", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessagesSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportMessagesSet_SupportTicketsSet_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTicketsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactionsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactionsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactionsSet_WalletsSet_WalletId",
                        column: x => x.WalletId,
                        principalTable: "WalletsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PricePerSeat = table.Column<decimal>(type: "numeric", nullable: false),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false),
                    ReferenceCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripsSet_DriversSet_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DriversSet",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripsSet_RoutesSet_RouteId",
                        column: x => x.RouteId,
                        principalTable: "RoutesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingsSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SeatCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    ReferenceCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingsSet_TripsSet_TripId",
                        column: x => x.TripId,
                        principalTable: "TripsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingsSet_UsersSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UsersSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoicesSet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicesSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicesSet_BookingsSet_BookingId",
                        column: x => x.BookingId,
                        principalTable: "BookingsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingsSet_TripId",
                table: "BookingsSet",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingsSet_UserId",
                table: "BookingsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DriversSet_UserId",
                table: "DriversSet",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriversSet_VehicleId",
                table: "DriversSet",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicesSet_BookingId",
                table: "InvoicesSet",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentsSet_Slug",
                table: "LegalDocumentsSet",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsSet_UserId",
                table: "NotificationsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokensSet_Token",
                table: "RefreshTokensSet",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokensSet_UserId",
                table: "RefreshTokensSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewsSet_UserId",
                table: "ReviewsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequestsSet_UserId",
                table: "RouteRequestsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLocationsSet_UserId",
                table: "SavedLocationsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StopsSet_RouteId",
                table: "StopsSet",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessagesSet_TicketId",
                table: "SupportMessagesSet",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketsSet_UserId",
                table: "SupportTicketsSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TripsSet_DriverId",
                table: "TripsSet",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_TripsSet_RouteId",
                table: "TripsSet",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersSet_Phone",
                table: "UsersSet",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletsSet_UserId",
                table: "WalletsSet",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactionsSet_WalletId",
                table: "WalletTransactionsSet",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqItemsSet");

            migrationBuilder.DropTable(
                name: "InvoicesSet");

            migrationBuilder.DropTable(
                name: "LegalDocumentsSet");

            migrationBuilder.DropTable(
                name: "NotificationsSet");

            migrationBuilder.DropTable(
                name: "OtpRequestsSet");

            migrationBuilder.DropTable(
                name: "RefreshTokensSet");

            migrationBuilder.DropTable(
                name: "ReviewsSet");

            migrationBuilder.DropTable(
                name: "RouteRequestsSet");

            migrationBuilder.DropTable(
                name: "SavedLocationsSet");

            migrationBuilder.DropTable(
                name: "StopsSet");

            migrationBuilder.DropTable(
                name: "SubscriptionPackagesSet");

            migrationBuilder.DropTable(
                name: "SupportMessagesSet");

            migrationBuilder.DropTable(
                name: "WalletTransactionsSet");

            migrationBuilder.DropTable(
                name: "BookingsSet");

            migrationBuilder.DropTable(
                name: "SupportTicketsSet");

            migrationBuilder.DropTable(
                name: "WalletsSet");

            migrationBuilder.DropTable(
                name: "TripsSet");

            migrationBuilder.DropTable(
                name: "DriversSet");

            migrationBuilder.DropTable(
                name: "RoutesSet");

            migrationBuilder.DropTable(
                name: "UsersSet");

            migrationBuilder.DropTable(
                name: "VehiclesSet");
        }
    }
}
