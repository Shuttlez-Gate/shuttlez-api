namespace Shuttlez.Application.Common.Interfaces;

public interface IDriverRealtimeNotifier
{
    Task NotifyTripsChangedAsync(Guid userId, CancellationToken cancellationToken = default);
}
