using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shuttlez.API.Hubs;

[AllowAnonymous]
public class TripTrackingHub : Hub
{
    public async Task JoinTrip(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task LeaveTrip(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip-{tripId}");
    }

    public async Task BroadcastDriverLocation(string tripId, double latitude, double longitude)
    {
        await Clients.Group($"trip-{tripId}").SendAsync("DriverLocationUpdated", new
        {
            tripId,
            latitude,
            longitude,
            timestamp = DateTime.UtcNow
        });
    }
}

[Authorize]
public class SupportChatHub : Hub
{
    public static string GroupName(Guid ticketId) => $"ticket-{ticketId}";
    public static string GroupName(string ticketId) => $"ticket-{ticketId}";

    public async Task JoinTicket(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            throw new HubException("معرّف التذكرة مطلوب");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(ticketId.Trim()));
    }

    public async Task LeaveTicket(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(ticketId.Trim()));
    }
}
