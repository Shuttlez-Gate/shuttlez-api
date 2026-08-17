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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
}
