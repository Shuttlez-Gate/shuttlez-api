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
    public DbSet<CommissionRule> CommissionRulesSet => Set<CommissionRule>();
    public DbSet<PricingRule> PricingRulesSet => Set<PricingRule>();
    public DbSet<UserDevice> UserDevicesSet => Set<UserDevice>();
    public DbSet<RideFareRule> RideFareRulesSet => Set<RideFareRule>();
    public DbSet<GroupFareRule> GroupFareRulesSet => Set<GroupFareRule>();
    public DbSet<RideRequest> RideRequestsSet => Set<RideRequest>();
    public DbSet<GroupRequest> GroupRequestsSet => Set<GroupRequest>();
    public DbSet<GroupMember> GroupMembersSet => Set<GroupMember>();

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
    public IQueryable<CommissionRule> CommissionRules => CommissionRulesSet.AsQueryable();
    public IQueryable<PricingRule> PricingRules => PricingRulesSet.AsQueryable();
    public IQueryable<UserDevice> UserDevices => UserDevicesSet.AsQueryable();
    public IQueryable<RideFareRule> RideFareRules => RideFareRulesSet.AsQueryable();
    public IQueryable<GroupFareRule> GroupFareRules => GroupFareRulesSet.AsQueryable();
    public IQueryable<RideRequest> RideRequests => RideRequestsSet.AsQueryable();
    public IQueryable<GroupRequest> GroupRequests => GroupRequestsSet.AsQueryable();
    public IQueryable<GroupMember> GroupMembers => GroupMembersSet.AsQueryable();
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

    public Task<int> TryDecrementTripSeatsAsync(
        Guid tripId,
        int seatCount,
        CancellationToken cancellationToken = default)
    {
        if (seatCount <= 0) return Task.FromResult(0);
        return TripsSet
            .Where(t =>
                t.Id == tripId &&
                !t.IsDeleted &&
                t.AvailableSeats >= seatCount &&
                (t.Status == Domain.Enums.TripStatus.Scheduled ||
                 t.Status == Domain.Enums.TripStatus.DriverAssigned))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.AvailableSeats, t => t.AvailableSeats - seatCount)
                    .SetProperty(t => t.UpdatedAt, _ => DateTime.UtcNow),
                cancellationToken);
    }

    public Task<int> TryIncrementTripSeatsAsync(
        Guid tripId,
        int seatCount,
        CancellationToken cancellationToken = default)
    {
        if (seatCount <= 0) return Task.FromResult(0);
        return TripsSet
            .Where(t => t.Id == tripId && !t.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.AvailableSeats, t => t.AvailableSeats + seatCount)
                    .SetProperty(t => t.UpdatedAt, _ => DateTime.UtcNow),
                cancellationToken);
    }

    public Task MarkTripFullIfNeededAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        return TripsSet
            .Where(t =>
                t.Id == tripId &&
                !t.IsDeleted &&
                t.AvailableSeats <= 0 &&
                t.Status == Domain.Enums.TripStatus.Scheduled)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.Status, _ => Domain.Enums.TripStatus.DriverAssigned)
                    .SetProperty(t => t.UpdatedAt, _ => DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public Task AcquireTransactionAdvisoryLockAsync(
        long lockKey,
        CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            new object[] { lockKey },
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleProviderId)
            .IsUnique()
            .HasFilter("\"GoogleProviderId\" IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.FacebookProviderId)
            .IsUnique()
            .HasFilter("\"FacebookProviderId\" IS NOT NULL");

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

        modelBuilder.Entity<RouteDemandGroupState>(s =>
        {
            s.HasIndex(x => x.RouteKey).IsUnique();
            s.HasIndex(x => x.MappedRouteId);
            s.HasOne(x => x.AssignedDriver)
                .WithMany()
                .HasForeignKey(x => x.AssignedDriverId)
                .OnDelete(DeleteBehavior.SetNull);
            s.HasOne(x => x.MappedRoute)
                .WithMany()
                .HasForeignKey(x => x.MappedRouteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Booking>(b =>
        {
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.PricePerSeat).HasPrecision(18, 2);
            b.Property(x => x.CommissionRate).HasPrecision(9, 6);
            b.Property(x => x.CommissionAmount).HasPrecision(18, 2);
            b.Property(x => x.CaptainEarnings).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Trip>(t =>
        {
            t.Property(x => x.PricePerSeat).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CommissionRule>(c =>
        {
            c.Property(x => x.PlatformCommissionPercent).HasPrecision(9, 4);
            c.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<PricingRule>(p =>
        {
            p.ToTable("PricingRulesSet");
            p.Property(x => x.Name).HasMaxLength(128);
            p.Property(x => x.OneWayPrice).HasPrecision(18, 2);
            p.Property(x => x.RoundTripPrice).HasPrecision(18, 2);
            p.Property(x => x.WeeklyPrice).HasPrecision(18, 2);
            p.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
            p.Property(x => x.LaunchCommissionPercent).HasPrecision(9, 4);
            p.Property(x => x.PermanentCommissionPercent).HasPrecision(9, 4);
            p.HasIndex(x => new { x.RouteId, x.VehicleType, x.IsActive });
            p.HasIndex(x => x.EffectiveFrom);
            p.HasIndex(x => x.EffectiveTo);
            p.HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Invoice>(i =>
        {
            i.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<UserDevice>(d =>
        {
            d.ToTable("UserDevicesSet");
            d.Property(x => x.Token).HasMaxLength(512).IsRequired();
            d.Property(x => x.Platform).HasMaxLength(32).IsRequired();
            d.HasIndex(x => x.Token);
            d.HasIndex(x => x.UserId);
            d.HasIndex(x => new { x.UserId, x.Token }).IsUnique();
            d.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RideFareRule>(r =>
        {
            r.ToTable("RideFareRulesSet");
            r.Property(x => x.Name).HasMaxLength(128);
            r.Property(x => x.FromZoneKey).HasMaxLength(64);
            r.Property(x => x.ToZoneKey).HasMaxLength(64);
            r.Property(x => x.FlatFare).HasPrecision(18, 2);
            r.Property(x => x.BaseFare).HasPrecision(18, 2);
            r.Property(x => x.PricePerKm).HasPrecision(18, 2);
            r.Property(x => x.MinimumFare).HasPrecision(18, 2);
            r.Property(x => x.MaximumFare).HasPrecision(18, 2);
            r.HasIndex(x => new { x.FromZoneKey, x.ToZoneKey, x.IsActive });
        });

        modelBuilder.Entity<GroupFareRule>(g =>
        {
            g.ToTable("GroupFareRulesSet");
            g.Property(x => x.Name).HasMaxLength(128);
            g.Property(x => x.FromZoneKey).HasMaxLength(64);
            g.Property(x => x.ToZoneKey).HasMaxLength(64);
            g.Property(x => x.CharterFlatFare).HasPrecision(18, 2);
            g.Property(x => x.BaseFare).HasPrecision(18, 2);
            g.Property(x => x.PricePerKm).HasPrecision(18, 2);
            g.Property(x => x.MinimumFare).HasPrecision(18, 2);
            g.Property(x => x.MaximumFare).HasPrecision(18, 2);
            g.HasIndex(x => new { x.FromZoneKey, x.ToZoneKey, x.IsActive });
        });

        modelBuilder.Entity<RideRequest>(r =>
        {
            r.ToTable("RideRequestsSet");
            r.Property(x => x.PickupAddress).HasMaxLength(512);
            r.Property(x => x.DestinationAddress).HasMaxLength(512);
            r.Property(x => x.FromZoneKey).HasMaxLength(64);
            r.Property(x => x.ToZoneKey).HasMaxLength(64);
            r.Property(x => x.PaymentMethod).HasMaxLength(32);
            r.Property(x => x.ReferenceCode).HasMaxLength(32);
            r.Property(x => x.FareAmount).HasPrecision(18, 2);
            r.Property(x => x.TotalAmount).HasPrecision(18, 2);
            r.Property(x => x.CommissionRate).HasPrecision(9, 6);
            r.Property(x => x.CommissionAmount).HasPrecision(18, 2);
            r.Property(x => x.CaptainEarnings).HasPrecision(18, 2);
            r.Property(x => x.DistanceKm).HasPrecision(18, 2);
            r.Property(x => x.BaseFareApplied).HasPrecision(18, 2);
            r.Property(x => x.PricePerKmApplied).HasPrecision(18, 2);
            r.Property(x => x.MinimumFareApplied).HasPrecision(18, 2);
            r.HasIndex(x => x.RiderUserId);
            r.HasIndex(x => x.DriverId);
            r.HasIndex(x => x.Status);
            r.HasOne(x => x.Rider)
                .WithMany()
                .HasForeignKey(x => x.RiderUserId)
                .OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);
            r.HasOne(x => x.RideFareRule)
                .WithMany()
                .HasForeignKey(x => x.RideFareRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupRequest>(g =>
        {
            g.ToTable("GroupRequestsSet");
            g.Property(x => x.PickupAddress).HasMaxLength(512);
            g.Property(x => x.DestinationAddress).HasMaxLength(512);
            g.Property(x => x.FromZoneKey).HasMaxLength(64);
            g.Property(x => x.ToZoneKey).HasMaxLength(64);
            g.Property(x => x.PaymentMethod).HasMaxLength(32);
            g.Property(x => x.ReferenceCode).HasMaxLength(32);
            g.Property(x => x.FareAmount).HasPrecision(18, 2);
            g.Property(x => x.TotalAmount).HasPrecision(18, 2);
            g.Property(x => x.CommissionRate).HasPrecision(9, 6);
            g.Property(x => x.CommissionAmount).HasPrecision(18, 2);
            g.Property(x => x.CaptainEarnings).HasPrecision(18, 2);
            g.Property(x => x.DistanceKm).HasPrecision(18, 2);
            g.Property(x => x.BaseFareApplied).HasPrecision(18, 2);
            g.Property(x => x.PricePerKmApplied).HasPrecision(18, 2);
            g.Property(x => x.MinimumFareApplied).HasPrecision(18, 2);
            g.HasIndex(x => x.OrganizerUserId);
            g.HasIndex(x => x.DriverId);
            g.HasIndex(x => x.Status);
            g.HasOne(x => x.Organizer)
                .WithMany()
                .HasForeignKey(x => x.OrganizerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            g.HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);
            g.HasOne(x => x.GroupFareRule)
                .WithMany()
                .HasForeignKey(x => x.GroupFareRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GroupMember>(m =>
        {
            m.ToTable("GroupMembersSet");
            m.HasIndex(x => new { x.GroupRequestId, x.UserId }).IsUnique();
            m.HasIndex(x => x.UserId);
            m.HasOne(x => x.GroupRequest)
                .WithMany(g => g.Members)
                .HasForeignKey(x => x.GroupRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            m.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
