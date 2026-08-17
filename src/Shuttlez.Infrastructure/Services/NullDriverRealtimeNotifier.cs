using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Infrastructure.Services;

public sealed class NullDriverRealtimeNotifier : IDriverRealtimeNotifier
{
    public Task NotifyTripsChangedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
