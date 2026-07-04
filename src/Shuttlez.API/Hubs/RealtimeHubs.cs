using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shuttlez.API.Hubs;

[Authorize]
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
    public async Task JoinTicket(string ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
    }

    public async Task SendMessage(string ticketId, string content)
    {
        await Clients.Group($"ticket-{ticketId}").SendAsync("MessageReceived", new
        {
            ticketId,
            content,
            senderId = Context.UserIdentifier,
            sentAt = DateTime.UtcNow
        });
    }
}
