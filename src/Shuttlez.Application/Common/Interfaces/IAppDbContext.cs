using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Common.Interfaces;

public interface IAppDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<OtpRequest> OtpRequests { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<SavedLocation> SavedLocations { get; }
    IQueryable<Trip> Trips { get; }
    IQueryable<Booking> Bookings { get; }
    IQueryable<Route> Routes { get; }
    IQueryable<Stop> Stops { get; }
    IQueryable<Notification> Notifications { get; }
    IQueryable<RouteRequest> RouteRequests { get; }
    IQueryable<SubscriptionPackage> SubscriptionPackages { get; }
    IQueryable<SupportTicket> SupportTickets { get; }
    IQueryable<SupportMessage> SupportMessages { get; }
    IQueryable<FaqItem> FaqItems { get; }
    IQueryable<LegalDocument> LegalDocuments { get; }
    IQueryable<LandingRouteLead> LandingRouteLeads { get; }
    IQueryable<LandingWaitlistEntry> LandingWaitlistEntries { get; }
    IQueryable<LandingCaptainLead> LandingCaptainLeads { get; }
    IQueryable<RouteDemandGroupState> RouteDemandGroupStates { get; }

    // مطلوبة للوحة التحكم (إدارة الأسطول والمحافظ والتقييمات).
    IQueryable<Driver> Drivers { get; }
    IQueryable<DriverDocument> DriverDocuments { get; }
    IQueryable<Vehicle> Vehicles { get; }
    IQueryable<Invoice> Invoices { get; }
    IQueryable<Wallet> Wallets { get; }
    IQueryable<WalletTransaction> WalletTransactions { get; }
    IQueryable<Review> Reviews { get; }
    IQueryable<CommissionRule> CommissionRules { get; }
    IQueryable<PricingRule> PricingRules { get; }
    IQueryable<UserDevice> UserDevices { get; }
    IQueryable<RideFareRule> RideFareRules { get; }
    IQueryable<GroupFareRule> GroupFareRules { get; }
    IQueryable<RideRequest> RideRequests { get; }
    IQueryable<GroupRequest> GroupRequests { get; }
    IQueryable<GroupMember> GroupMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;

    /// Atomic seat reservation. Returns rows affected (0 = unavailable).
    Task<int> TryDecrementTripSeatsAsync(
        Guid tripId,
        int seatCount,
        CancellationToken cancellationToken = default);

    /// Restores seats after cancel / compensating failure.
    Task<int> TryIncrementTripSeatsAsync(
        Guid tripId,
        int seatCount,
        CancellationToken cancellationToken = default);

    /// Sets DriverAssigned when seats are depleted after reservation.
    Task MarkTripFullIfNeededAsync(Guid tripId, CancellationToken cancellationToken = default);

    /// Runs work inside a DB transaction (Serializable) for booking integrity.
    Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// PostgreSQL transaction-scoped advisory lock (must be called inside a transaction).
    Task AcquireTransactionAdvisoryLockAsync(long lockKey, CancellationToken cancellationToken = default);
}
