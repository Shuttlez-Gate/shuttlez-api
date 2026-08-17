using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> UsersSet => Set<User>();
    public DbSet<OtpRequest> OtpRequestsSet => Set<OtpRequest>();
    public DbSet<RefreshToken> RefreshTokensSet => Set<RefreshToken>();
    public DbSet<SavedLocation> SavedLocationsSet => Set<SavedLocation>();
    public DbSet<Trip> TripsSet => Set<Trip>();
    public DbSet<Booking> BookingsSet => Set<Booking>();
    public DbSet<Route> RoutesSet => Set<Route>();
    public DbSet<Notification> NotificationsSet => Set<Notification>();
    public DbSet<FaqItem> FaqItemsSet => Set<FaqItem>();
    public DbSet<LegalDocument> LegalDocumentsSet => Set<LegalDocument>();
    public DbSet<Driver> DriversSet => Set<Driver>();
    public DbSet<DriverDocument> DriverDocumentsSet => Set<DriverDocument>();
    public DbSet<Vehicle> VehiclesSet => Set<Vehicle>();
    public DbSet<Stop> StopsSet => Set<Stop>();
    public DbSet<Invoice> InvoicesSet => Set<Invoice>();
    public DbSet<Wallet> WalletsSet => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactionsSet => Set<WalletTransaction>();
    public DbSet<Review> ReviewsSet => Set<Review>();
    public DbSet<SupportTicket> SupportTicketsSet => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessagesSet => Set<SupportMessage>();
    public DbSet<RouteRequest> RouteRequestsSet => Set<RouteRequest>();
    public DbSet<SubscriptionPackage> SubscriptionPackagesSet => Set<SubscriptionPackage>();
    public DbSet<LandingRouteLead> LandingRouteLeadsSet => Set<LandingRouteLead>();
    public DbSet<LandingWaitlistEntry> LandingWaitlistEntriesSet => Set<LandingWaitlistEntry>();
    public DbSet<LandingCaptainLead> LandingCaptainLeadsSet => Set<LandingCaptainLead>();
    public DbSet<RouteDemandGroupState> RouteDemandGroupStatesSet => Set<RouteDemandGroupState>();

    public IQueryable<User> Users => UsersSet.AsQueryable();
    public IQueryable<OtpRequest> OtpRequests => OtpRequestsSet.AsQueryable();
    public IQueryable<RefreshToken> RefreshTokens => RefreshTokensSet.AsQueryable();
    public IQueryable<SavedLocation> SavedLocations => SavedLocationsSet.AsQueryable();
    public IQueryable<Trip> Trips => TripsSet.AsQueryable();
    public IQueryable<Booking> Bookings => BookingsSet.AsQueryable();
    public IQueryable<Route> Routes => RoutesSet.AsQueryable();
    public IQueryable<Stop> Stops => StopsSet.AsQueryable();
    public IQueryable<Notification> Notifications => NotificationsSet.AsQueryable();
    public IQueryable<RouteRequest> RouteRequests => RouteRequestsSet.AsQueryable();
    public IQueryable<SubscriptionPackage> SubscriptionPackages => SubscriptionPackagesSet.AsQueryable();
    public IQueryable<SupportTicket> SupportTickets => SupportTicketsSet.AsQueryable();
    public IQueryable<SupportMessage> SupportMessages => SupportMessagesSet.AsQueryable();
    public IQueryable<FaqItem> FaqItems => FaqItemsSet.AsQueryable();
    public IQueryable<LegalDocument> LegalDocuments => LegalDocumentsSet.AsQueryable();
    public IQueryable<LandingRouteLead> LandingRouteLeads => LandingRouteLeadsSet.AsQueryable();
    public IQueryable<LandingWaitlistEntry> LandingWaitlistEntries => LandingWaitlistEntriesSet.AsQueryable();
    public IQueryable<LandingCaptainLead> LandingCaptainLeads => LandingCaptainLeadsSet.AsQueryable();
    public IQueryable<RouteDemandGroupState> RouteDemandGroupStates => RouteDemandGroupStatesSet.AsQueryable();
    public IQueryable<Driver> Drivers => DriversSet.AsQueryable();
    public IQueryable<DriverDocument> DriverDocuments => DriverDocumentsSet.AsQueryable();
    public IQueryable<Vehicle> Vehicles => VehiclesSet.AsQueryable();
    public IQueryable<Invoice> Invoices => InvoicesSet.AsQueryable();
    public IQueryable<Wallet> Wallets => WalletsSet.AsQueryable();
    public IQueryable<WalletTransaction> WalletTransactions => WalletTransactionsSet.AsQueryable();
    public IQueryable<Review> Reviews => ReviewsSet.AsQueryable();

    public new void Add<T>(T entity) where T : class => Set<T>().Add(entity);
    public new void Update<T>(T entity) where T : class => Set<T>().Update(entity);
    public new void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();

        modelBuilder.Entity<LegalDocument>()
            .HasIndex(l => l.Slug)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Invoice)
            .WithOne(i => i.Booking)
            .HasForeignKey<Invoice>(i => i.BookingId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Wallet)
            .WithOne(w => w.User)
            .HasForeignKey<Wallet>(w => w.UserId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Driver)
            .WithOne(d => d.User)
            .HasForeignKey<Driver>(d => d.UserId);

        modelBuilder.Entity<DriverDocument>()
            .HasOne(d => d.Driver)
            .WithMany(d => d.Documents)
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DriverDocument>()
            .HasIndex(d => d.DriverId);

        modelBuilder.Entity<RouteDemandGroupState>()
            .HasIndex(s => s.RouteKey)
            .IsUnique();

        modelBuilder.Entity<RouteDemandGroupState>()
            .HasOne(s => s.AssignedDriver)
            .WithMany()
            .HasForeignKey(s => s.AssignedDriverId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
